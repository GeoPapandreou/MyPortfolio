using System.Text.Json;
using System.Text.RegularExpressions;

namespace MyPortfolioAPI.Utilities;

public static class FileMapParser
{
    private static readonly string[] ContainerPropertyNames = ["files", "fileMap", "output", "result", "data"];
    private static readonly string[] PathPropertyNames = ["path", "filePath", "file", "name"];
    private static readonly string[] ContentPropertyNames = ["content", "contentLines", "lines", "text", "value", "body", "source"];
    private static readonly Regex DelimitedFilePattern = new(
        @"<<<FILE:(?<path>[^\r\n>]+)>>>\s*(?<content>.*?)\s*<<<END FILE>>>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    public static bool TryParse(string rawContent, out Dictionary<string, string> files)
    {
        files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return false;
        }

        if (!TryParseJsonElement(Normalize(rawContent), out var root))
        {
            return TryParseDelimited(rawContent, out files);
        }

        if (!TryExtractFiles(root, out var parsedFiles) || parsedFiles.Count == 0)
        {
            return TryParseDelimited(rawContent, out files);
        }

        files = parsedFiles.ToDictionary(
            pair => pair.Key.Replace('\\', '/').Trim(),
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        return true;
    }

    private static bool TryParseJsonElement(string json, out JsonElement root)
    {
        root = default;

        try
        {
            using var document = JsonDocument.Parse(json);
            root = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryExtractFiles(JsonElement element, out Dictionary<string, string> files)
    {
        files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryReadSingleFileObject(element, out files))
                {
                    return files.Count > 0;
                }

                if (TryReadFlexibleDictionary(element, out files))
                {
                    return files.Count > 0;
                }

                foreach (var propertyName in ContainerPropertyNames)
                {
                    if (TryGetPropertyIgnoreCase(element, propertyName, out var nestedElement) &&
                        TryExtractFiles(nestedElement, out files) &&
                        files.Count > 0)
                    {
                        return true;
                    }
                }

                return false;

            case JsonValueKind.Array:
                return TryReadFileArray(element, out files);

            case JsonValueKind.String:
                var nestedJson = Normalize(element.GetString() ?? string.Empty);
                return TryParseJsonElement(nestedJson, out var nestedRoot) && TryExtractFiles(nestedRoot, out files);

            default:
                return false;
        }
    }

    private static bool TryReadSingleFileObject(JsonElement element, out Dictionary<string, string> files)
    {
        files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!TryReadPathValue(element, out var path) || !TryReadContentValue(element, out var content))
        {
            return false;
        }

        files[path] = content;
        return true;
    }

    private static bool TryReadFlexibleDictionary(JsonElement element, out Dictionary<string, string> files)
    {
        files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            if (!TryReadInlineContent(property.Value, out var content))
            {
                return false;
            }

            files[property.Name] = content;
        }

        return files.Count > 0;
    }

    private static bool TryReadFileArray(JsonElement element, out Dictionary<string, string> files)
    {
        files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !TryReadPathValue(item, out var path) ||
                !TryReadContentValue(item, out var content))
            {
                return false;
            }

            files[path] = content;
        }

        return files.Count > 0;
    }

    private static bool TryReadPathValue(JsonElement element, out string path)
    {
        path = string.Empty;

        foreach (var propertyName in PathPropertyNames)
        {
            if (TryGetPropertyIgnoreCase(element, propertyName, out var propertyValue) &&
                propertyValue.ValueKind == JsonValueKind.String)
            {
                path = propertyValue.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(path);
            }
        }

        return false;
    }

    private static bool TryReadInlineContent(JsonElement element, out string content)
    {
        content = string.Empty;

        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                content = element.GetString() ?? string.Empty;
                return true;

            case JsonValueKind.Array:
                var lines = element.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToList();

                if (lines.Count > 0)
                {
                    content = string.Join('\n', lines);
                    return true;
                }

                return false;

            case JsonValueKind.Object:
                return TryReadContentValue(element, out content);

            default:
                return false;
        }
    }

    private static bool TryReadContentValue(JsonElement element, out string content)
    {
        content = string.Empty;

        foreach (var propertyName in ContentPropertyNames)
        {
            if (!TryGetPropertyIgnoreCase(element, propertyName, out var propertyValue))
            {
                continue;
            }

            switch (propertyValue.ValueKind)
            {
                case JsonValueKind.String:
                    content = propertyValue.GetString() ?? string.Empty;
                    return true;

                case JsonValueKind.Array:
                    var lines = propertyValue.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetString() ?? string.Empty)
                        .ToList();

                    if (lines.Count > 0)
                    {
                        content = string.Join('\n', lines);
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryParseDelimited(string rawContent, out Dictionary<string, string> files)
    {
        files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var matches = DelimitedFilePattern.Matches(rawContent);
        if (matches.Count == 0)
        {
            return false;
        }

        foreach (Match match in matches)
        {
            var path = match.Groups["path"].Value.Replace('\\', '/').Trim();
            var content = match.Groups["content"].Value
                .Replace("\r\n", "\n")
                .Trim('\r', '\n');

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            files[path] = content;
        }

        return files.Count > 0;
    }

    private static string Normalize(string rawContent)
    {
        var trimmed = rawContent.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBreak = trimmed.IndexOf('\n');
            if (firstBreak >= 0)
            {
                trimmed = trimmed[(firstBreak + 1)..];
            }

            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
            {
                trimmed = trimmed[..lastFence];
            }
        }

        var arrayStart = trimmed.IndexOf('[');
        var objectStart = trimmed.IndexOf('{');

        if (arrayStart >= 0 && (objectStart < 0 || arrayStart < objectStart))
        {
            var arrayEnd = trimmed.LastIndexOf(']');
            if (arrayEnd > arrayStart)
            {
                trimmed = trimmed[arrayStart..(arrayEnd + 1)];
            }
        }
        else if (objectStart >= 0)
        {
            var objectEnd = trimmed.LastIndexOf('}');
            if (objectEnd > objectStart)
            {
                trimmed = trimmed[objectStart..(objectEnd + 1)];
            }
        }

        return trimmed.Trim();
    }
}
