using Microsoft.Extensions.Options;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Exceptions;
using MyPortfolioAPI.Options;
using MyPortfolioAPI.Utilities;

namespace MyPortfolioAPI.Services;

public interface IPortfolioGenerationService
{
    Task<GeneratedPortfolioResult> GenerateAsync(UserProfileDto profile, ReferenceImageDto? referenceImage = null, CancellationToken cancellationToken = default);
}

public sealed class PortfolioGenerationService : IPortfolioGenerationService
{
    private readonly IGeminiService _geminiService;
    private readonly GeminiOptions _geminiOptions;
    private readonly IPortfolioFrontendPromptBuilder _promptBuilder;
    private readonly IGeneratedFrontendPackageValidator _frontendPackageValidator;
    private readonly IGeneratedPortfolioReadmeBuilder _readmeBuilder;
    private readonly ILogger<PortfolioGenerationService> _logger;
    private readonly IZipService _zipService;

    public PortfolioGenerationService(
        IGeminiService geminiService,
        IOptions<GeminiOptions> geminiOptions,
        IPortfolioFrontendPromptBuilder promptBuilder,
        IGeneratedFrontendPackageValidator frontendPackageValidator,
        IGeneratedPortfolioReadmeBuilder readmeBuilder,
        IZipService zipService,
        ILogger<PortfolioGenerationService> logger)
    {
        _geminiService = geminiService;
        _geminiOptions = geminiOptions.Value;
        _promptBuilder = promptBuilder;
        _frontendPackageValidator = frontendPackageValidator;
        _readmeBuilder = readmeBuilder;
        _zipService = zipService;
        _logger = logger;
    }

    public async Task<GeneratedPortfolioResult> GenerateAsync(UserProfileDto profile, ReferenceImageDto? referenceImage = null, CancellationToken cancellationToken = default)
    {
        var frontendFiles = await BuildFrontendFilesAsync(profile, referenceImage, cancellationToken);
        var backendFiles = GeneratedBackendTemplates.BuildBackendFiles(profile);

        var manifest = new GeneratedPortfolioManifest
        {
            FrontendFiles = frontendFiles,
            BackendFiles = backendFiles,
            ReadmeContent = _readmeBuilder.Build(profile)
        };

        var zipBytes = await _zipService.CreateArchiveAsync(manifest, cancellationToken);

        return new GeneratedPortfolioResult
        {
            Manifest = manifest,
            ZipBytes = zipBytes
        };
    }

    private async Task<Dictionary<string, string>> BuildFrontendFilesAsync(UserProfileDto profile, ReferenceImageDto? referenceImage, CancellationToken cancellationToken)
    {
        return await GenerateAiFrontendFilesAsync(profile, referenceImage, cancellationToken);
    }

    private async Task<Dictionary<string, string>> GenerateAiFrontendFilesAsync(UserProfileDto profile, ReferenceImageDto? referenceImage, CancellationToken cancellationToken)
    {
        if (!GeminiOptions.HasConfiguredApiKey(_geminiOptions.ApiKey))
        {
            throw new ClientSafeException(
                "Frontend generation requires a Gemini API key. Add the Gemini configuration and try again.",
                StatusCodes.Status503ServiceUnavailable);
        }

        var prompt = _promptBuilder.BuildPrimaryPrompt(profile, referenceImage);
        var repairPrompt = _promptBuilder.BuildRepairPrompt(profile, referenceImage);
        var delimitedPrompt = _promptBuilder.BuildDelimitedPrompt(profile, referenceImage);
        var inlineMedia = ToGeminiInlineMedia(referenceImage);

        try
        {
            var response = await _geminiService.GenerateContentAsync(prompt, inlineMedia, cancellationToken);
            if (_frontendPackageValidator.TryParseAndValidate(response, out var files, out var failureReason))
            {
                _logger.LogInformation("Portfolio generation used an AI-generated frontend package.");
                return files;
            }

            _logger.LogWarning(
                "AI frontend generation returned invalid content on the first attempt. Reason: {Reason}. Response excerpt: {ResponseExcerpt}",
                failureReason,
                CreateExcerpt(response));

            response = await _geminiService.GenerateContentAsync(repairPrompt, inlineMedia, cancellationToken);
            if (_frontendPackageValidator.TryParseAndValidate(response, out files, out failureReason))
            {
                _logger.LogInformation("Portfolio generation used an AI-generated frontend package after repair.");
                return files;
            }

            _logger.LogWarning(
                "AI frontend generation returned invalid content on the repair attempt. Reason: {Reason}. Response excerpt: {ResponseExcerpt}",
                failureReason,
                CreateExcerpt(response));

            response = await _geminiService.GenerateContentAsync(delimitedPrompt, responseMimeType: null, inlineMedia, cancellationToken);
            if (_frontendPackageValidator.TryParseAndValidate(response, out files, out failureReason))
            {
                _logger.LogInformation("Portfolio generation used an AI-generated frontend package after delimited fallback.");
                return files;
            }

            _logger.LogWarning(
                "AI frontend generation returned invalid content on the delimited fallback attempt. Reason: {Reason}. Response excerpt: {ResponseExcerpt}",
                failureReason,
                CreateExcerpt(response));

            throw new ClientSafeException(
                "Frontend generation returned invalid AI output after multiple attempts. Please try again.",
                StatusCodes.Status502BadGateway);
        }
        catch (ClientSafeException exception)
        {
            _logger.LogWarning(
                exception,
                "AI frontend generation could not be completed safely.");
            throw;
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "AI frontend generation returned unusable content.");
            throw new ClientSafeException(
                "Frontend generation returned unusable AI output. Please try again.",
                StatusCodes.Status502BadGateway);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected failure while generating the AI frontend package.");
            throw new ClientSafeException(
                "Frontend generation could not be completed right now. Please try again in a moment.",
                StatusCodes.Status502BadGateway);
        }
    }

    private static GeminiInlineMediaInput? ToGeminiInlineMedia(ReferenceImageDto? referenceImage)
    {
        if (referenceImage is null ||
            string.IsNullOrWhiteSpace(referenceImage.MimeType) ||
            string.IsNullOrWhiteSpace(referenceImage.Base64Data))
        {
            return null;
        }

        return new GeminiInlineMediaInput(referenceImage.MimeType, referenceImage.Base64Data);
    }

    private static string CreateExcerpt(string value)
    {
        const int maxLength = 900;
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<empty>";
        }

        var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return normalized.Length <= maxLength ? normalized : $"{normalized[..maxLength]}...";
    }
}
