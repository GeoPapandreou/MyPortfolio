using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MyPortfolioAPI.Exceptions;
using MyPortfolioAPI.Options;

namespace MyPortfolioAPI.Services;

public interface IGeminiService
{
    Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default);
    Task<string> GenerateContentAsync(string prompt, string? responseMimeType, CancellationToken cancellationToken = default);
    Task<string> GenerateContentAsync(string prompt, GeminiInlineMediaInput? inlineMedia, CancellationToken cancellationToken = default);
    Task<string> GenerateContentAsync(string prompt, string? responseMimeType, GeminiInlineMediaInput? inlineMedia, CancellationToken cancellationToken = default);
}

public sealed class GeminiInlineMediaInput
{
    public GeminiInlineMediaInput(string mimeType, string base64Data)
    {
        MimeType = mimeType.Trim();
        Base64Data = base64Data.Trim();
    }

    public string MimeType { get; }

    public string Base64Data { get; }
}

public sealed class GeminiService : IGeminiService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private readonly GeminiOptions _options;

    public GeminiService(IHttpClientFactory httpClientFactory, IOptions<GeminiOptions> options, ILogger<GeminiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        return await SendAsync(prompt, "application/json", inlineMedia: null, allowRetry: true, cancellationToken);
    }

    public async Task<string> GenerateContentAsync(string prompt, string? responseMimeType, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        return await SendAsync(prompt, responseMimeType, inlineMedia: null, allowRetry: true, cancellationToken);
    }

    public async Task<string> GenerateContentAsync(string prompt, GeminiInlineMediaInput? inlineMedia, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        return await SendAsync(prompt, "application/json", inlineMedia, allowRetry: true, cancellationToken);
    }

    public async Task<string> GenerateContentAsync(string prompt, string? responseMimeType, GeminiInlineMediaInput? inlineMedia, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        return await SendAsync(prompt, responseMimeType, inlineMedia, allowRetry: true, cancellationToken);
    }

    private async Task<string> SendAsync(string prompt, string? responseMimeType, GeminiInlineMediaInput? inlineMedia, bool allowRetry, CancellationToken cancellationToken)
    {
        var endpoint = $"{_options.Endpoint}?key={_options.ApiKey}";
        var generationConfig = new Dictionary<string, object?>
        {
            ["temperature"] = _options.Temperature,
            ["maxOutputTokens"] = _options.MaxOutputTokens
        };

        if (!string.IsNullOrWhiteSpace(responseMimeType))
        {
            generationConfig["responseMimeType"] = responseMimeType;
        }

        var parts = new List<object>
        {
            new
            {
                text = prompt
            }
        };

        if (inlineMedia is not null)
        {
            parts.Add(new
            {
                inlineData = new
                {
                    mimeType = inlineMedia.MimeType,
                    data = inlineMedia.Base64Data
                }
            });
        }

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts
                }
            },
            generationConfig
        };

        using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, SerializerOptions, cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests && allowRetry)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return await SendAsync(prompt, responseMimeType, inlineMedia, allowRetry: false, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var providerMessage = TryExtractProviderErrorMessage(responseBody);
            var safeLogMessage = CreateSafeProviderLogMessage(providerMessage);

            _logger.LogWarning(
                "Gemini request failed with status code {StatusCode}. {SafeProviderMessage}",
                (int)response.StatusCode,
                safeLogMessage);

            throw CreateClientSafeException(response.StatusCode, providerMessage);
        }

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var responseModel = await JsonSerializer.DeserializeAsync<GeminiResponse>(responseStream, SerializerOptions, cancellationToken);
        var text = responseModel?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        return string.IsNullOrWhiteSpace(text)
            ? throw new InvalidOperationException("Gemini returned an empty response.")
            : text;
    }

    private sealed class GeminiResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    private sealed class Candidate
    {
        public Content? Content { get; set; }
    }

    private sealed class Content
    {
        public List<Part>? Parts { get; set; }
    }

    private sealed class Part
    {
        public string? Text { get; set; }
    }

    private static ClientSafeException CreateClientSafeException(HttpStatusCode statusCode, string? providerMessage)
    {
        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            return new ClientSafeException("Portfolio generation is busy right now. Please try again in a moment.", StatusCodes.Status429TooManyRequests);
        }

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden || LooksLikeConfigurationIssue(providerMessage))
        {
            return new ClientSafeException(
                "Portfolio generation is not configured correctly. Update the Gemini API key and model settings, then try again.",
                StatusCodes.Status503ServiceUnavailable);
        }

        if (statusCode == HttpStatusCode.BadRequest && LooksLikePromptSizeIssue(providerMessage))
        {
            return new ClientSafeException(
                "Your portfolio details are too large to generate right now. Try shortening longer fields like your bio, project descriptions, or responsibilities and try again.",
                StatusCodes.Status400BadRequest);
        }

        return new ClientSafeException(
            "Portfolio generation could not be completed right now. Please try again in a moment.",
            StatusCodes.Status502BadGateway);
    }

    private void EnsureConfigured()
    {
        if (!GeminiOptions.HasConfiguredApiKey(_options.ApiKey))
        {
            throw new ClientSafeException(
                "Portfolio generation is not configured yet. Add a Gemini API key and try again.",
                StatusCodes.Status503ServiceUnavailable);
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            !Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpointUri) ||
            endpointUri.Scheme is not ("http" or "https"))
        {
            throw new ClientSafeException(
                "Portfolio generation is not configured correctly. Update the Gemini endpoint and try again.",
                StatusCodes.Status503ServiceUnavailable);
        }

        if (_options.MaxOutputTokens <= 0)
        {
            throw new ClientSafeException(
                "Portfolio generation is not configured correctly. Update the Gemini token limits and try again.",
                StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static bool LooksLikeConfigurationIssue(string? providerMessage)
    {
        if (string.IsNullOrWhiteSpace(providerMessage))
        {
            return false;
        }

        return providerMessage.Contains("api key", StringComparison.OrdinalIgnoreCase) ||
               providerMessage.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) ||
               providerMessage.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
               providerMessage.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
               providerMessage.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikePromptSizeIssue(string? providerMessage)
    {
        if (string.IsNullOrWhiteSpace(providerMessage))
        {
            return false;
        }

        return providerMessage.Contains("token", StringComparison.OrdinalIgnoreCase) ||
               providerMessage.Contains("too long", StringComparison.OrdinalIgnoreCase) ||
               providerMessage.Contains("too large", StringComparison.OrdinalIgnoreCase) ||
               providerMessage.Contains("context", StringComparison.OrdinalIgnoreCase) ||
               providerMessage.Contains("size", StringComparison.OrdinalIgnoreCase) ||
               providerMessage.Contains("length", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateSafeProviderLogMessage(string? providerMessage)
    {
        if (LooksLikeConfigurationIssue(providerMessage))
        {
            return "Gemini reported a configuration or permission problem.";
        }

        if (LooksLikePromptSizeIssue(providerMessage))
        {
            return "Gemini reported a prompt size limit problem.";
        }

        return string.IsNullOrWhiteSpace(providerMessage)
            ? "Gemini returned no additional error details."
            : "Gemini returned an unclassified provider error.";
    }

    private static string? TryExtractProviderErrorMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            var response = JsonSerializer.Deserialize<GeminiErrorResponse>(responseBody, SerializerOptions);
            return response?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class GeminiErrorResponse
    {
        public GeminiError? Error { get; set; }
    }

    private sealed class GeminiError
    {
        public string? Message { get; set; }
    }
}
