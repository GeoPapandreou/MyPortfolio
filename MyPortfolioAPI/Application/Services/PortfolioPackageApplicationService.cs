using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Exceptions;
using MyPortfolioAPI.Models;
using MyPortfolioAPI.Utilities;

namespace MyPortfolioAPI.Services;

public interface IPortfolioPackageApplicationService
{
    Task<PortfolioPackageFileResult> GenerateAsync(Guid userId, GenerateRequestDto request, CancellationToken cancellationToken = default);

    Task<PortfolioPackageFileResult> DownloadVersionAsync(Guid userId, Guid versionId, CancellationToken cancellationToken = default);
}

public sealed record PortfolioPackageFileResult(byte[] FileBytes, string FileName);

public sealed class PortfolioPackageApplicationService : IPortfolioPackageApplicationService
{
    private readonly IPortfolioGenerationService _portfolioGenerationService;
    private readonly IPortfolioPersistenceService _portfolioPersistenceService;
    private readonly IPortfolioVersionRepository _portfolioVersionRepository;
    private readonly IPortfolioArtifactStorage _artifactStorage;

    public PortfolioPackageApplicationService(
        IPortfolioGenerationService portfolioGenerationService,
        IPortfolioPersistenceService portfolioPersistenceService,
        IPortfolioVersionRepository portfolioVersionRepository,
        IPortfolioArtifactStorage artifactStorage)
    {
        _portfolioGenerationService = portfolioGenerationService;
        _portfolioPersistenceService = portfolioPersistenceService;
        _portfolioVersionRepository = portfolioVersionRepository;
        _artifactStorage = artifactStorage;
    }

    public async Task<PortfolioPackageFileResult> GenerateAsync(Guid userId, GenerateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Profile is null)
        {
            throw new ClientSafeException("Your answers are required before a portfolio can be built.", StatusCodes.Status400BadRequest);
        }

        var profileToPersist = UserProfileSanitizer.CreatePersistenceSafeCopy(request.Profile);
        var portfolio = await _portfolioPersistenceService.SaveAsync(userId, profileToPersist, cancellationToken);
        var nextVersionNumber = portfolio.Versions.Count > 0
            ? portfolio.Versions.Max(item => item.VersionNumber) + 1
            : 1;

        var generatedResult = await _portfolioGenerationService.GenerateAsync(request.Profile, request.ReferenceImage, cancellationToken);
        var versionId = Guid.NewGuid();
        var generatedAt = DateTime.UtcNow;
        var zipRelativePath = await _artifactStorage.PersistAsync(userId, versionId, generatedResult.Manifest, generatedResult.ZipBytes, cancellationToken);

        try
        {
            await _portfolioVersionRepository.AddAsync(new PortfolioVersion
            {
                Id = versionId,
                PortfolioId = portfolio.Id,
                VersionNumber = nextVersionNumber,
                GeneratedAt = generatedAt,
                ZipUrl = zipRelativePath
            }, cancellationToken);
        }
        catch
        {
            _artifactStorage.DeletePersistedArtifacts(userId, versionId);
            throw;
        }

        return new PortfolioPackageFileResult(generatedResult.ZipBytes, BuildVersionFileName(nextVersionNumber));
    }

    public async Task<PortfolioPackageFileResult> DownloadVersionAsync(Guid userId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var portfolio = await _portfolioPersistenceService.LoadAsync(userId, cancellationToken);
        if (portfolio is null)
        {
            throw new ClientSafeException("No saved portfolio could be found for this account.", StatusCodes.Status404NotFound);
        }

        var version = portfolio.Versions.FirstOrDefault(item => item.Id == versionId);
        if (version is null)
        {
            throw new ClientSafeException("The selected portfolio version could not be found.", StatusCodes.Status404NotFound);
        }

        var fileBytes = await _artifactStorage.ReadVersionPackageAsync(userId, version.Id, version.ZipUrl, cancellationToken);
        if (fileBytes is null)
        {
            throw new ClientSafeException("The saved portfolio package could not be found.", StatusCodes.Status404NotFound);
        }

        return new PortfolioPackageFileResult(fileBytes, BuildVersionFileName(version.VersionNumber));
    }

    private static string BuildVersionFileName(int versionNumber)
    {
        return $"MyPortfolio_{Math.Max(1, versionNumber)}.zip";
    }
}
