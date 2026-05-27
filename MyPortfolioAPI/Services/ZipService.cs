using System.IO.Compression;
using System.Text;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Utilities;

namespace MyPortfolioAPI.Services;

public interface IZipService
{
    Task<byte[]> CreateArchiveAsync(GeneratedPortfolioManifest manifest, CancellationToken cancellationToken = default);
}

public sealed class ZipService : IZipService
{
    public async Task<byte[]> CreateArchiveAsync(GeneratedPortfolioManifest manifest, CancellationToken cancellationToken = default)
    {
        await using var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            await AddEntriesAsync(archive, GeneratedProjectType.Frontend, "MyPortfolioUI", manifest.FrontendFiles, cancellationToken);
            await AddEntriesAsync(archive, GeneratedProjectType.Backend, "MyPortfolioAPI", manifest.BackendFiles, cancellationToken);
            await AddEntryAsync(archive, "README.md", manifest.ReadmeContent, cancellationToken);
        }

        return memoryStream.ToArray();
    }

    private static async Task AddEntriesAsync(
        ZipArchive archive,
        GeneratedProjectType projectType,
        string rootFolder,
        IReadOnlyDictionary<string, string> files,
        CancellationToken cancellationToken)
    {
        foreach (var entry in files.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            var normalizedPath = GeneratedFilePathSanitizer.SanitizeRelativePath(entry.Key, projectType);
            await AddEntryAsync(archive, $"{rootFolder}/{normalizedPath}", entry.Value, cancellationToken);
        }
    }

    private static async Task AddEntryAsync(
        ZipArchive archive,
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        await using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await using var entryStream = entry.Open();
        await contentStream.CopyToAsync(entryStream, cancellationToken);
    }
}
