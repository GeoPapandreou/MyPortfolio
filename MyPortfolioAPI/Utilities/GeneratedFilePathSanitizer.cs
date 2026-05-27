using System.Text.RegularExpressions;

namespace MyPortfolioAPI.Utilities;

public enum GeneratedProjectType
{
    Frontend,
    Backend
}

public static partial class GeneratedFilePathSanitizer
{
    private static readonly HashSet<string> FrontendTopLevelDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "src",
        "public",
        "assets",
        "tests"
    };

    private static readonly HashSet<string> BackendTopLevelDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Controllers",
        "Models",
        "Data",
        "Properties",
        "Services",
        "Extensions",
        "DTOs",
        "Options",
        "Validation",
        "Utilities",
        "Middleware",
        "Helpers",
        "Interfaces",
        "Repositories",
        "Mappings",
        "Mapping"
    };

    private static readonly HashSet<string> BlockedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "node_modules",
        "bin",
        "obj",
        "dist"
    };

    private static readonly HashSet<string> AllowedRootDotFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ".editorconfig",
        ".gitignore",
        ".env",
        ".env.example"
    };

    private static readonly HashSet<string> AllowedRootFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".csproj",
        ".css",
        ".cjs",
        ".html",
        ".js",
        ".json",
        ".jsx",
        ".md",
        ".mjs",
        ".png",
        ".props",
        ".ps1",
        ".sln",
        ".svg",
        ".targets",
        ".ts",
        ".tsx",
        ".txt",
        ".xml",
        ".yaml",
        ".yml"
    };

    public static Dictionary<string, string> SanitizeFileMap(Dictionary<string, string> files, GeneratedProjectType projectType)
    {
        var sanitizedFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var sanitizedPath = SanitizeRelativePath(file.Key, projectType);
            sanitizedFiles[sanitizedPath] = file.Value;
        }

        if (sanitizedFiles.Count == 0)
        {
            throw new InvalidOperationException("The portfolio generator did not return any valid files.");
        }

        return sanitizedFiles;
    }

    public static string SanitizeRelativePath(string rawPath, GeneratedProjectType projectType)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            throw new InvalidOperationException("Generated files must include a valid relative path.");
        }

        var normalizedPath = Normalize(rawPath, projectType);
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            throw new InvalidOperationException($"The generated file path '{rawPath}' is invalid.");
        }

        foreach (var segment in segments)
        {
            ValidateSegment(segment, rawPath);
        }

        if (segments.Any(segment => BlockedDirectoryNames.Contains(segment)))
        {
            throw new InvalidOperationException($"The generated file path '{rawPath}' targets a blocked directory.");
        }

        if (segments.Length == 1)
        {
            ValidateRootFile(segments[0], rawPath);
            return segments[0];
        }

        var allowedTopLevelDirectories = projectType == GeneratedProjectType.Frontend
            ? FrontendTopLevelDirectories
            : BackendTopLevelDirectories;

        if (!allowedTopLevelDirectories.Contains(segments[0]))
        {
            throw new InvalidOperationException(
                $"The generated file path '{rawPath}' is outside the allowed {projectType.ToString().ToLowerInvariant()} project structure.");
        }

        return string.Join('/', segments);
    }

    private static string Normalize(string rawPath, GeneratedProjectType projectType)
    {
        var normalizedPath = rawPath.Replace('\\', '/').Trim();

        while (normalizedPath.StartsWith("./", StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath[2..];
        }

        var projectRoot = projectType == GeneratedProjectType.Frontend ? "MyPortfolioUI/" : "MyPortfolioAPI/";
        if (normalizedPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath[projectRoot.Length..];
        }

        normalizedPath = normalizedPath.TrimStart('/');

        if (string.IsNullOrWhiteSpace(normalizedPath) ||
            normalizedPath.StartsWith("//", StringComparison.Ordinal) ||
            normalizedPath.Contains(':', StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The generated file path '{rawPath}' is not a safe project-relative path.");
        }

        return normalizedPath;
    }

    private static void ValidateSegment(string segment, string rawPath)
    {
        if (segment is "." or "..")
        {
            throw new InvalidOperationException($"The generated file path '{rawPath}' contains path traversal.");
        }

        if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || segment.Any(char.IsControl))
        {
            throw new InvalidOperationException($"The generated file path '{rawPath}' contains invalid characters.");
        }

        if (!SafeSegmentPattern().IsMatch(segment))
        {
            throw new InvalidOperationException($"The generated file path '{rawPath}' contains unsupported characters.");
        }
    }

    private static void ValidateRootFile(string fileName, string rawPath)
    {
        if (AllowedRootDotFiles.Contains(fileName))
        {
            return;
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedRootFileExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"The generated file path '{rawPath}' is not an allowed project root file.");
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeSegmentPattern();
}
