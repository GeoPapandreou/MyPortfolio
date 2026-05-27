using System.Text.Json;
using MyPortfolioAPI.DTOs;

namespace MyPortfolioAPI.Services;

public interface IPortfolioArtifactStorage
{
    Task<string> PersistAsync(
        Guid userId,
        Guid versionId,
        GeneratedPortfolioManifest manifest,
        byte[] zipBytes,
        CancellationToken cancellationToken = default);

    Task<byte[]?> ReadVersionPackageAsync(
        Guid userId,
        Guid versionId,
        string zipUrl,
        CancellationToken cancellationToken = default);

    void DeletePersistedArtifacts(Guid userId, Guid versionId);

    void DeleteVersionArtifacts(Guid userId, Guid versionId, string zipUrl);

    void DeleteUserArtifacts(Guid userId);
}

public sealed class PortfolioArtifactStorage : IPortfolioArtifactStorage
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PortfolioArtifactStorage> _logger;

    public PortfolioArtifactStorage(IWebHostEnvironment environment, ILogger<PortfolioArtifactStorage> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> PersistAsync(
        Guid userId,
        Guid versionId,
        GeneratedPortfolioManifest manifest,
        byte[] zipBytes,
        CancellationToken cancellationToken = default)
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
            TryDeleteArtifacts(
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

    public async Task<byte[]?> ReadVersionPackageAsync(
        Guid userId,
        Guid versionId,
        string zipUrl,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveArtifactPath(userId, zipUrl, out var zipPath))
        {
            _logger.LogWarning(
                "Skipped downloading portfolio artifact for version {VersionId} because stored path '{ZipUrl}' is outside the expected artifact root for user {UserId}.",
                versionId,
                zipUrl,
                userId);
            return null;
        }

        if (!System.IO.File.Exists(zipPath))
        {
            return null;
        }

        return await System.IO.File.ReadAllBytesAsync(zipPath, cancellationToken);
    }

    public void DeletePersistedArtifacts(Guid userId, Guid versionId)
    {
        var artifactsRoot = GetUserArtifactsRoot(userId);
        var zipPath = Path.Combine(artifactsRoot, $"{versionId:N}.zip");
        var manifestPath = Path.Combine(artifactsRoot, $"{versionId:N}.manifest.json");
        var tempZipPath = $"{zipPath}.tmp";
        var tempManifestPath = $"{manifestPath}.tmp";

        TryDeleteArtifacts(
            artifactsRoot,
            zipPath,
            manifestPath,
            tempZipPath,
            tempManifestPath,
            versionId,
            "Generated portfolio artifacts for version {VersionId} could not be cleaned up after version persistence failed.");
    }

    public void DeleteVersionArtifacts(Guid userId, Guid versionId, string zipUrl)
    {
        if (!TryResolveArtifactPaths(userId, zipUrl, out var zipPath, out var manifestPath, out var artifactsDirectory))
        {
            _logger.LogWarning(
                "Skipped deleting portfolio artifact files for version {VersionId} because stored path '{ZipUrl}' is outside the expected artifact root for user {UserId}.",
                versionId,
                zipUrl,
                userId);
            return;
        }

        try
        {
            if (System.IO.File.Exists(zipPath))
            {
                System.IO.File.Delete(zipPath);
            }

            if (System.IO.File.Exists(manifestPath))
            {
                System.IO.File.Delete(manifestPath);
            }

            if (Directory.Exists(artifactsDirectory) &&
                !Directory.EnumerateFileSystemEntries(artifactsDirectory).Any())
            {
                Directory.Delete(artifactsDirectory);
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                exception,
                "Portfolio version {VersionId} was removed from the database, but its saved artifact files could not be deleted yet.",
                versionId);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(
                exception,
                "Portfolio version {VersionId} was removed from the database, but its saved artifact files could not be deleted yet.",
                versionId);
        }
    }

    public void DeleteUserArtifacts(Guid userId)
    {
        var artifactsRoot = GetUserArtifactsRoot(userId);
        if (!Directory.Exists(artifactsRoot))
        {
            return;
        }

        try
        {
            Directory.Delete(artifactsRoot, true);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                exception,
                "User {UserId} was deleted from the database, but saved portfolio artifacts could not be removed yet.",
                userId);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(
                exception,
                "User {UserId} was deleted from the database, but saved portfolio artifacts could not be removed yet.",
                userId);
        }
    }

    private void TryDeleteArtifacts(
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
            _logger.LogWarning(exception, warningMessage, versionId);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, warningMessage, versionId);
        }
    }

    private bool TryResolveArtifactPaths(Guid userId, string zipUrl, out string zipPath, out string manifestPath, out string artifactsDirectory)
    {
        zipPath = string.Empty;
        manifestPath = string.Empty;
        artifactsDirectory = string.Empty;

        if (string.IsNullOrWhiteSpace(zipUrl))
        {
            return false;
        }

        var artifactsRoot = GetUserArtifactsRoot(userId);
        var resolvedZipPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, zipUrl));
        if (!IsPathWithinRoot(resolvedZipPath, artifactsRoot))
        {
            return false;
        }

        var resolvedArtifactsDirectory = Path.GetDirectoryName(resolvedZipPath);
        if (string.IsNullOrWhiteSpace(resolvedArtifactsDirectory) || !IsPathWithinRoot(resolvedArtifactsDirectory, artifactsRoot))
        {
            return false;
        }

        var resolvedManifestPath = Path.Combine(
            resolvedArtifactsDirectory,
            $"{Path.GetFileNameWithoutExtension(resolvedZipPath)}.manifest.json");

        if (!IsPathWithinRoot(resolvedManifestPath, artifactsRoot))
        {
            return false;
        }

        zipPath = resolvedZipPath;
        manifestPath = resolvedManifestPath;
        artifactsDirectory = resolvedArtifactsDirectory;
        return true;
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
