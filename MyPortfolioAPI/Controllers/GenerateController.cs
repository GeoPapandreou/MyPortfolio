using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPortfolioAPI.Data;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Extensions;
using MyPortfolioAPI.Models;
using MyPortfolioAPI.Services;
using MyPortfolioAPI.Utilities;

namespace MyPortfolioAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolio")]
public sealed class GenerateController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPortfolioGenerationService _portfolioGenerationService;
    private readonly IPortfolioPersistenceService _portfolioPersistenceService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<GenerateController> _logger;

    public GenerateController(
        AppDbContext dbContext,
        IPortfolioGenerationService portfolioGenerationService,
        IPortfolioPersistenceService portfolioPersistenceService,
        IWebHostEnvironment environment,
        ILogger<GenerateController> logger)
    {
        _dbContext = dbContext;
        _portfolioGenerationService = portfolioGenerationService;
        _portfolioPersistenceService = portfolioPersistenceService;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateAsync(GenerateRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Profile is null)
        {
            return BadRequest(new { message = "Your answers are required before a portfolio can be built." });
        }

        var userId = User.GetUserId();
        var profileToPersist = UserProfileSanitizer.CreatePersistenceSafeCopy(request.Profile);
        var portfolio = await _portfolioPersistenceService.SaveAsync(userId, profileToPersist, cancellationToken);
        var portfolioId = portfolio.Id;
        var nextVersionNumber = portfolio.Versions.Count > 0
            ? portfolio.Versions.Max(item => item.VersionNumber) + 1
            : 1;

        var generatedResult = await _portfolioGenerationService.GenerateAsync(request.Profile, request.ReferenceImage, cancellationToken);
        var versionId = Guid.NewGuid();
        var generatedAt = DateTime.UtcNow;
        var zipRelativePath = await PersistArtifactsAsync(userId, versionId, generatedResult.Manifest, generatedResult.ZipBytes, cancellationToken);

        try
        {
            _dbContext.ChangeTracker.Clear();
            _dbContext.PortfolioVersions.Add(new PortfolioVersion
            {
                Id = versionId,
                PortfolioId = portfolioId,
                VersionNumber = nextVersionNumber,
                GeneratedAt = generatedAt,
                ZipUrl = zipRelativePath
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _dbContext.ChangeTracker.Clear();
            TryDeletePersistedArtifacts(userId, versionId);
            throw;
        }

        var fileName = BuildVersionFileName(nextVersionNumber);
        return File(generatedResult.ZipBytes, "application/zip", fileName);
    }

    [HttpGet("versions/{versionId:guid}/download")]
    public async Task<IActionResult> DownloadVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var portfolio = await _portfolioPersistenceService.LoadAsync(userId, cancellationToken);
        if (portfolio is null)
        {
            return NotFound(new { message = "No saved portfolio could be found for this account." });
        }

        var version = portfolio.Versions.FirstOrDefault(item => item.Id == versionId);
        if (version is null)
        {
            return NotFound(new { message = "The selected portfolio version could not be found." });
        }

        if (!TryResolveArtifactPath(userId, version.ZipUrl, out var zipPath))
        {
            _logger.LogWarning(
                "Skipped downloading portfolio artifact for version {VersionId} because stored path '{ZipUrl}' is outside the expected artifact root for user {UserId}.",
                version.Id,
                version.ZipUrl,
                userId);
            return NotFound(new { message = "The saved portfolio package could not be found." });
        }

        if (!System.IO.File.Exists(zipPath))
        {
            return NotFound(new { message = "The saved portfolio package could not be found." });
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(zipPath, cancellationToken);
        var fileName = BuildVersionFileName(version.VersionNumber);
        return File(fileBytes, "application/zip", fileName);
    }

    private async Task<string> PersistArtifactsAsync(
        Guid userId,
        Guid versionId,
        GeneratedPortfolioManifest manifest,
        byte[] zipBytes,
        CancellationToken cancellationToken)
    {
        var artifactsRoot = Path.Combine(_environment.ContentRootPath, "GeneratedArtifacts", userId.ToString("N"));
        Directory.CreateDirectory(artifactsRoot);

        var zipPath = Path.Combine(artifactsRoot, $"{versionId:N}.zip");
        var manifestPath = Path.Combine(artifactsRoot, $"{versionId:N}.manifest.json");
        var tempZipPath = $"{zipPath}.tmp";
        var tempManifestPath = $"{manifestPath}.tmp";
        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });

        try
        {
            await System.IO.File.WriteAllBytesAsync(tempZipPath, zipBytes, cancellationToken);
            await System.IO.File.WriteAllTextAsync(tempManifestPath, manifestJson, cancellationToken);

            System.IO.File.Move(tempZipPath, zipPath);
            System.IO.File.Move(tempManifestPath, manifestPath);
        }
        catch
        {
            TryDeletePersistedArtifacts(
                artifactsRoot,
                zipPath,
                manifestPath,
                tempZipPath,
                tempManifestPath,
                versionId,
                "Generated portfolio artifacts could not be fully written and were being cleaned up.");
            throw;
        }

        return Path.GetRelativePath(_environment.ContentRootPath, zipPath);
    }

    private void TryDeletePersistedArtifacts(Guid userId, Guid versionId)
    {
        var artifactsRoot = GetUserArtifactsRoot(userId);
        var zipPath = Path.Combine(artifactsRoot, $"{versionId:N}.zip");
        var manifestPath = Path.Combine(artifactsRoot, $"{versionId:N}.manifest.json");
        var tempZipPath = $"{zipPath}.tmp";
        var tempManifestPath = $"{manifestPath}.tmp";

        TryDeletePersistedArtifacts(
            artifactsRoot,
            zipPath,
            manifestPath,
            tempZipPath,
            tempManifestPath,
            versionId,
            "Generated portfolio artifacts for version {VersionId} could not be cleaned up after version persistence failed.");
    }

    private void TryDeletePersistedArtifacts(
        string artifactsRoot,
        string zipPath,
        string manifestPath,
        string tempZipPath,
        string tempManifestPath,
        Guid versionId,
        string warningMessage)
    {
        var pathsToDelete = new[] { zipPath, manifestPath, tempZipPath, tempManifestPath };

        try
        {
            foreach (var path in pathsToDelete)
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            if (Directory.Exists(artifactsRoot) &&
                !Directory.EnumerateFileSystemEntries(artifactsRoot).Any())
            {
                Directory.Delete(artifactsRoot);
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                exception,
                warningMessage,
                versionId);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(
                exception,
                warningMessage,
                versionId);
        }
    }

    private static string BuildVersionFileName(int versionNumber)
    {
        return $"MyPortfolio_{Math.Max(1, versionNumber)}.zip";
    }

    private bool TryResolveArtifactPath(Guid userId, string zipUrl, out string zipPath)
    {
        zipPath = string.Empty;

        if (string.IsNullOrWhiteSpace(zipUrl))
        {
            return false;
        }

        var artifactsRoot = GetUserArtifactsRoot(userId);
        var resolvedZipPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, zipUrl));
        if (!IsPathWithinRoot(resolvedZipPath, artifactsRoot) ||
            !string.Equals(Path.GetExtension(resolvedZipPath), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        zipPath = resolvedZipPath;
        return true;
    }

    private string GetUserArtifactsRoot(Guid userId)
    {
        return Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath,
            "GeneratedArtifacts",
            userId.ToString("N")));
    }

    private static bool IsPathWithinRoot(string candidatePath, string rootPath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var normalizedRoot = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(candidatePath, normalizedRoot, comparison) ||
               candidatePath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison);
    }
}
