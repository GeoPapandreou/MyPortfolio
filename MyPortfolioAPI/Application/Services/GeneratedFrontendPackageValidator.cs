using System.Text.Json;
using System.Text.RegularExpressions;
using MyPortfolioAPI.Utilities;

namespace MyPortfolioAPI.Services;

public interface IGeneratedFrontendPackageValidator
{
    bool TryParseAndValidate(string rawContent, out Dictionary<string, string> frontendFiles, out string failureReason);
}

public sealed partial class GeneratedFrontendPackageValidator : IGeneratedFrontendPackageValidator
{
    private static readonly string[] RequiredFrontendFiles =
    [
        "package.json",
        "vite.config.js",
        "index.html",
        "src/main.jsx",
        "src/App.jsx",
        "src/index.css"
    ];

    private static readonly HashSet<string> AllowedDependencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "react",
        "react-dom"
    };

    private static readonly HashSet<string> AllowedDevDependencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "vite",
        "@vitejs/plugin-react",
        "@types/react",
        "@types/react-dom",
        "eslint",
        "eslint-plugin-react",
        "eslint-plugin-react-hooks",
        "eslint-plugin-react-refresh"
    };

    private static readonly HashSet<string> AllowedImportSpecifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "react",
        "react-dom",
        "react-dom/client",
        "react/jsx-runtime",
        "vite",
        "@vitejs/plugin-react"
    };

    private static readonly string[] RelativeImportExtensions =
    [
        "",
        ".js",
        ".jsx",
        ".mjs",
        ".cjs",
        ".json",
        ".css",
        ".svg",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    ];

    public bool TryParseAndValidate(
        string rawContent,
        out Dictionary<string, string> frontendFiles,
        out string failureReason)
    {
        frontendFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        failureReason = string.Empty;

        if (!FileMapParser.TryParse(rawContent, out var parsedFiles))
        {
            failureReason = "The response could not be parsed into a file map.";
            return false;
        }

        Dictionary<string, string> sanitizedFiles;
        try
        {
            sanitizedFiles = GeneratedFilePathSanitizer.SanitizeFileMap(parsedFiles, GeneratedProjectType.Frontend);
        }
        catch (InvalidOperationException exception)
        {
            failureReason = exception.Message;
            return false;
        }

        foreach (var requiredFile in RequiredFrontendFiles)
        {
            if (!sanitizedFiles.ContainsKey(requiredFile))
            {
                failureReason = $"The generated frontend is missing '{requiredFile}'.";
                return false;
            }
        }

        if (!TryValidatePackageJson(sanitizedFiles["package.json"], out failureReason))
        {
            return false;
        }

        if (!ContainsPortfolioApiReference(sanitizedFiles))
        {
            failureReason = "The generated frontend does not appear to load data from /api/portfolio.";
            return false;
        }

        if (!TryValidateImports(sanitizedFiles, out failureReason))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(sanitizedFiles["src/App.jsx"]) ||
            string.IsNullOrWhiteSpace(sanitizedFiles["src/main.jsx"]) ||
            string.IsNullOrWhiteSpace(sanitizedFiles["src/index.css"]))
        {
            failureReason = "The generated frontend contains an empty required source file.";
            return false;
        }

        frontendFiles = sanitizedFiles;
        return true;
    }

    private static bool TryValidatePackageJson(string packageJson, out string failureReason)
    {
        failureReason = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(packageJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                failureReason = "package.json is not a valid JSON object.";
                return false;
            }

            if (!HasOnlyAllowedPackageKeys(root, "dependencies", AllowedDependencies, out failureReason) ||
                !HasOnlyAllowedPackageKeys(root, "devDependencies", AllowedDevDependencies, out failureReason))
            {
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            failureReason = "package.json is not valid JSON.";
            return false;
        }
    }

    private static bool HasOnlyAllowedPackageKeys(
        JsonElement root,
        string propertyName,
        HashSet<string> allowedKeys,
        out string failureReason)
    {
        failureReason = string.Empty;

        if (!root.TryGetProperty(propertyName, out var section))
        {
            return true;
        }

        if (section.ValueKind != JsonValueKind.Object)
        {
            failureReason = $"package.json {propertyName} must be an object.";
            return false;
        }

        foreach (var property in section.EnumerateObject())
        {
            if (!allowedKeys.Contains(property.Name))
            {
                failureReason = $"package.json includes the unsupported package '{property.Name}'.";
                return false;
            }
        }

        return true;
    }

    private static bool ContainsPortfolioApiReference(IReadOnlyDictionary<string, string> files)
    {
        return files
            .Where(file => IsScriptFile(file.Key))
            .Select(file => file.Value)
            .Any(content =>
                content.Contains("/api/portfolio", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("localhost:5000/api/portfolio", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryValidateImports(
        IReadOnlyDictionary<string, string> files,
        out string failureReason)
    {
        failureReason = string.Empty;
        var normalizedPaths = new HashSet<string>(files.Keys, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (!IsScriptFile(file.Key))
            {
                continue;
            }

            foreach (var importSpecifier in EnumerateImportSpecifiers(file.Value))
            {
                if (string.IsNullOrWhiteSpace(importSpecifier))
                {
                    continue;
                }

                if (importSpecifier.StartsWith(".", StringComparison.Ordinal) ||
                    importSpecifier.StartsWith("/", StringComparison.Ordinal))
                {
                    if (!ResolveRelativeImport(file.Key, importSpecifier, normalizedPaths))
                    {
                        failureReason = $"The generated frontend references a missing local import '{importSpecifier}' from '{file.Key}'.";
                        return false;
                    }

                    continue;
                }

                if (!AllowedImportSpecifiers.Contains(importSpecifier))
                {
                    failureReason = $"The generated frontend references the unsupported import '{importSpecifier}'.";
                    return false;
                }
            }
        }

        return true;
    }

    private static IEnumerable<string> EnumerateImportSpecifiers(string content)
    {
        foreach (Match match in StaticImportPattern().Matches(content))
        {
            yield return match.Groups["spec"].Value;
        }

        foreach (Match match in ExportFromPattern().Matches(content))
        {
            yield return match.Groups["spec"].Value;
        }

        foreach (Match match in DynamicImportPattern().Matches(content))
        {
            yield return match.Groups["spec"].Value;
        }

        foreach (Match match in RequirePattern().Matches(content))
        {
            yield return match.Groups["spec"].Value;
        }
    }

    private static bool ResolveRelativeImport(string filePath, string importSpecifier, HashSet<string> files)
    {
        var normalizedImport = importSpecifier.Replace('\\', '/');
        if (normalizedImport.StartsWith("/", StringComparison.Ordinal))
        {
            return ResolveCandidate(normalizedImport.TrimStart('/'), files);
        }

        var currentDirectory = filePath.Contains('/')
            ? filePath[..filePath.LastIndexOf('/')]
            : string.Empty;

        var basePath = string.IsNullOrWhiteSpace(currentDirectory)
            ? normalizedImport
            : $"{currentDirectory}/{normalizedImport}";

        var resolvedPath = NormalizePath(basePath);
        return ResolveCandidate(resolvedPath, files);
    }

    private static bool ResolveCandidate(string basePath, HashSet<string> files)
    {
        foreach (var extension in RelativeImportExtensions)
        {
            var candidate = basePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                ? basePath
                : $"{basePath}{extension}";

            if (files.Contains(candidate))
            {
                return true;
            }
        }

        return files.Contains($"{basePath}/index.js") ||
               files.Contains($"{basePath}/index.jsx") ||
               files.Contains($"{basePath}/index.mjs") ||
               files.Contains($"{basePath}/index.css");
    }

    private static string NormalizePath(string path)
    {
        var segments = new List<string>();

        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    return string.Empty;
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return string.Join('/', segments);
    }

    private static bool IsScriptFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension is ".js" or ".jsx" or ".mjs" or ".cjs";
    }

    [GeneratedRegex("""\bimport\s+(?:[^;]*?\s+from\s+)?["'](?<spec>[^"']+)["']""", RegexOptions.CultureInvariant)]
    private static partial Regex StaticImportPattern();

    [GeneratedRegex("""\bexport\s+[^;]*?\s+from\s+["'](?<spec>[^"']+)["']""", RegexOptions.CultureInvariant)]
    private static partial Regex ExportFromPattern();

    [GeneratedRegex("""\bimport\(\s*["'](?<spec>[^"']+)["']\s*\)""", RegexOptions.CultureInvariant)]
    private static partial Regex DynamicImportPattern();

    [GeneratedRegex("""\brequire\(\s*["'](?<spec>[^"']+)["']\s*\)""", RegexOptions.CultureInvariant)]
    private static partial Regex RequirePattern();
}
