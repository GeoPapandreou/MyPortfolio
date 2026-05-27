using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

public sealed partial class PortfolioGenerationService : IPortfolioGenerationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
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

    private readonly IGeminiService _geminiService;
    private readonly GeminiOptions _geminiOptions;
    private readonly ILogger<PortfolioGenerationService> _logger;
    private readonly IZipService _zipService;

    public PortfolioGenerationService(
        IGeminiService geminiService,
        IOptions<GeminiOptions> geminiOptions,
        IZipService zipService,
        ILogger<PortfolioGenerationService> logger)
    {
        _geminiService = geminiService;
        _geminiOptions = geminiOptions.Value;
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
            ReadmeContent = BuildReadme(profile)
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

        var prompt = BuildAiFrontendPrompt(profile, referenceImage);
        var repairPrompt = BuildAiFrontendRepairPrompt(profile, referenceImage);
        var delimitedPrompt = BuildAiFrontendDelimitedPrompt(profile, referenceImage);
        var inlineMedia = ToGeminiInlineMedia(referenceImage);

        try
        {
            var response = await _geminiService.GenerateContentAsync(prompt, inlineMedia, cancellationToken);
            if (TryParseAndValidateFrontend(response, out var files, out var failureReason))
            {
                _logger.LogInformation("Portfolio generation used an AI-generated frontend package.");
                return files;
            }

            _logger.LogWarning(
                "AI frontend generation returned invalid content on the first attempt. Reason: {Reason}. Response excerpt: {ResponseExcerpt}",
                failureReason,
                CreateExcerpt(response));

            response = await _geminiService.GenerateContentAsync(repairPrompt, inlineMedia, cancellationToken);
            if (TryParseAndValidateFrontend(response, out files, out failureReason))
            {
                _logger.LogInformation("Portfolio generation used an AI-generated frontend package after repair.");
                return files;
            }

            _logger.LogWarning(
                "AI frontend generation returned invalid content on the repair attempt. Reason: {Reason}. Response excerpt: {ResponseExcerpt}",
                failureReason,
                CreateExcerpt(response));

            response = await _geminiService.GenerateContentAsync(delimitedPrompt, responseMimeType: null, inlineMedia, cancellationToken);
            if (TryParseAndValidateFrontend(response, out files, out failureReason))
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

    private static bool TryParseAndValidateFrontend(
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

    private static string BuildAiFrontendPrompt(UserProfileDto profile, ReferenceImageDto? referenceImage)
    {
        var promptProfile = UserProfileSanitizer.CreatePersistenceSafeCopy(profile);
        var profileJson = JsonSerializer.Serialize(promptProfile, SerializerOptions);
        var themeDirection = BuildThemeDirection(profile.Theme);
        var referenceImageGuidance = BuildReferenceImageGuidance(referenceImage);

        return $$"""
                You are a senior React frontend engineer and a strong digital art director.
                Generate a complete portfolio frontend that feels intentionally designed, production-ready, and visually close to the supplied template direction rather than like a generic portfolio starter.

                Your priorities, in order:
                1. Preserve the reference image's macro layout and design language as closely as possible when a reference image is provided.
                2. Create a polished portfolio with deliberate hierarchy, spacing, typography, and composition.
                3. Map the user's real portfolio content into that design without inventing fake content.
                4. Keep the code simple, reliable, and easy to run locally.
                5. Gracefully handle missing fields without collapsing into a bland fallback layout.

                Stack and output rules:
                - Use React 18 with Vite.
                - Use plain CSS only. No Tailwind CSS.
                - Do not use react-router-dom, TypeScript, icon libraries, UI kits, animation libraries, or packages beyond:
                  - react
                  - react-dom
                  - vite
                  - @vitejs/plugin-react
                  - optionally, only these extra dev-only packages if truly necessary:
                    - @types/react
                    - @types/react-dom
                    - eslint
                    - eslint-plugin-react
                    - eslint-plugin-react-hooks
                    - eslint-plugin-react-refresh
                - Do not include any other package.
                - Return exactly these files and no others:
                  - package.json
                  - vite.config.js
                  - index.html
                  - src/main.jsx
                  - src/App.jsx
                  - src/index.css
                - All 6 files are mandatory.
                - src/main.jsx must import "./index.css".
                - Keep most of the UI in src/App.jsx.
                - Define reusable CSS variables for colors, spacing, type, and surfaces.
                - Include responsive breakpoints for tablet and mobile layouts, and preserve the design's hierarchy on mobile instead of turning it into a generic vertical stack.
                - Do not move styling inline into JSX.
                - Keep package.json minimal. Only include scripts for dev, build, and preview unless a lint script is truly necessary.
                - Do not include comments.

                Data and API rules:
                - The portfolio must fetch its data from /api/portfolio through an API base URL variable.
                - Use `const API_BASE_URL = (import.meta.env.VITE_API_URL ?? "http://localhost:5000").replace(/\/$/, "")` or an equivalent fallback.
                - Then fetch from `${API_BASE_URL}/api/portfolio`.
                - Do not hardcode the profile into the React code.
                - Use the real API field names exactly.
                - Handle missing or empty fields gracefully.
                - If there is no profile photo, render a designed fallback such as initials, a monogram, or an abstract portrait treatment. Do not use placeholder image services.

                Design rules:
                - Think in terms of art direction first, not component defaults.
                - Establish a strong hero section with a clear visual focal point.
                - Preserve the template's section ordering, column structure, density, alignment logic, whitespace rhythm, and relative scale relationships whenever possible.
                - Preserve the balance between text-heavy and visual-heavy areas.
                - If the template has asymmetry, layered panels, editorial spacing, or dense premium grouping, keep those traits.
                - If the template is restrained, do not add noisy effects. If it is expressive, do not flatten it into a safe layout.
                - Use the user's real content and map it into the closest analogous places in the design.
                - It is acceptable to rename section headings for better fit, but the content must still come from the provided data.
                - If a section has sparse data, preserve the design structure and adapt the presentation elegantly instead of deleting the structure and reverting to a generic layout.
                - Do not invent testimonials, blog posts, product metrics, client logos, case studies, fake analytics, fake download links, or fake social networks that are not present in the data.
                - Do not add CTA buttons that imply missing assets such as a resume download unless the underlying URL or data exists.
                - Avoid the default centered hero plus simple card grid unless the reference image clearly uses that composition.
                - Avoid generic white background plus random blue accent styling unless the direction explicitly calls for it.
                - Avoid repeated identical cards with equal spacing everywhere.
                - Avoid dashboard-like panels unless the template clearly has that language.
                - Avoid stock startup-site patterns, fake device mockups, or filler decorations that do not serve the chosen composition.
                - The result should feel like a custom portfolio someone intentionally designed for this person.
                - Create one memorable hero moment, one strong secondary rhythm for the rest of the page, and a cohesive top-to-bottom visual system.
                - Typography should feel deliberate, with meaningful contrast between headline, body, labels, and metadata.
                - Surfaces, borders, shadows, gradients, and accents should be used intentionally and consistently, not randomly.
                - If a reference image is attached, treat it as the primary design template. The theme direction is secondary and should support the reference image instead of overriding it.

                Data contract from the API:
                - personalInfo.fullName
                - personalInfo.profession
                - personalInfo.bio
                - personalInfo.photoUrl
                - personalInfo.location
                - experiences[].organisation
                - experiences[].role
                - experiences[].startDate
                - experiences[].endDate
                - experiences[].isCurrent
                - experiences[].bullets[]
                - workSamples[].title
                - workSamples[].description
                - workSamples[].tools[]
                - workSamples[].liveUrl
                - contactInfo.email
                - contactInfo.phone
                - contactInfo.linkedIn
                - contactInfo.instagram
                - contactInfo.facebook
                - contactInfo.gitHub

                Theme direction:
                {{themeDirection}}

                {{referenceImageGuidance}}

                Return only valid JSON as an array of file objects with this exact shape:
                [
                  {
                    "path": "src/App.jsx",
                    "content": [
                      "line 1",
                      "line 2"
                    ]
                  }
                ]

                Rules for the JSON:
                - Do not use markdown fences.
                - Each file path must be relative.
                - Each content value must be an array of strings, one line per string.
                - Escape JSON correctly.

                Portfolio data to design for:
                {{profileJson}}
                """;
    }

    private static string BuildAiFrontendRepairPrompt(UserProfileDto profile, ReferenceImageDto? referenceImage)
    {
        var promptProfile = UserProfileSanitizer.CreatePersistenceSafeCopy(profile);
        var profileJson = JsonSerializer.Serialize(promptProfile, SerializerOptions);
        var themeDirection = BuildThemeDirection(profile.Theme);
        var referenceImageGuidance = BuildReferenceImageGuidance(referenceImage);

        return $$"""
                The previous answer was invalid.
                Try again and be stricter while keeping the design quality high.

                Return exactly these 6 files:
                - package.json
                - vite.config.js
                - index.html
                - src/main.jsx
                - src/App.jsx
                - src/index.css

                Hard requirements:
                - React + Vite only.
                - Plain CSS only.
                - No extra dependencies or imports outside react, react-dom, vite, and @vitejs/plugin-react.
                - Avoid TypeScript, @types packages, linting packages, test packages, Prettier, Tailwind, and other tooling unless absolutely necessary.
                - If you do include extra dev-only packages, they must be limited to:
                  - @types/react
                  - @types/react-dom
                  - eslint
                  - eslint-plugin-react
                  - eslint-plugin-react-hooks
                  - eslint-plugin-react-refresh
                - Fetch data from `${(import.meta.env.VITE_API_URL ?? "http://localhost:5000").replace(/\/$/, "")}/api/portfolio` or an equivalent environment-based fallback.
                - Do not hardcode only one absolute fetch URL with no environment fallback.
                - Use the real API field names exactly.
                - Return all 6 required files.
                - src/index.css is mandatory and src/main.jsx must import "./index.css".
                - Do not move styling inline into JSX.
                - Include responsive CSS breakpoints.
                - Do not use placeholder image services.
                - Return only valid JSON as an array of file objects.
                - Do not include markdown fences or explanations.
                - Treat this as a serious art-direction task, not a generic starter template.
                - Preserve the reference image's macro layout, spacing rhythm, hierarchy, and content density as closely as possible when it exists.
                - Avoid the default centered hero plus simple card grid unless the reference image clearly uses that structure.
                - Preserve a strong hero composition and a cohesive visual system through the rest of the page.
                - Do not invent fake sections or fake content to fill empty space.
                - If a reference image is attached, treat it as the primary design template. The theme direction is secondary and should support the reference image instead of overriding it.

                Theme direction:
                {{themeDirection}}

                {{referenceImageGuidance}}

                Portfolio data:
                {{profileJson}}
                """;
    }

    private static string BuildAiFrontendDelimitedPrompt(UserProfileDto profile, ReferenceImageDto? referenceImage)
    {
        var promptProfile = UserProfileSanitizer.CreatePersistenceSafeCopy(profile);
        var profileJson = JsonSerializer.Serialize(promptProfile, SerializerOptions);
        var themeDirection = BuildThemeDirection(profile.Theme);
        var referenceImageGuidance = BuildReferenceImageGuidance(referenceImage);

        return $$"""
                Return plain text only using this exact format for every file:

                <<<FILE:relative/path.ext>>>
                full file contents here
                <<<END FILE>>>

                Return exactly these files:
                - package.json
                - vite.config.js
                - index.html
                - src/main.jsx
                - src/App.jsx
                - src/index.css

                Requirements:
                - React + Vite only.
                - Plain CSS only.
                - No extra packages beyond react, react-dom, vite, and @vitejs/plugin-react.
                - Avoid TypeScript, @types packages, linting packages, test packages, Prettier, Tailwind, and other tooling unless absolutely necessary.
                - If you do include extra dev-only packages, they must be limited to:
                  - @types/react
                  - @types/react-dom
                  - eslint
                  - eslint-plugin-react
                  - eslint-plugin-react-hooks
                  - eslint-plugin-react-refresh
                - Fetch data from `${(import.meta.env.VITE_API_URL ?? "http://localhost:5000").replace(/\/$/, "")}/api/portfolio` or an equivalent environment-based fallback.
                - Do not hardcode only one absolute fetch URL with no environment fallback.
                - Use the real API field names exactly.
                - All 6 files are mandatory.
                - src/index.css must exist and src/main.jsx must import "./index.css".
                - Do not move styling inline into JSX.
                - Include responsive CSS breakpoints.
                - Do not use placeholder image services.
                - Preserve the reference image's macro layout, spacing rhythm, hierarchy, and density as closely as possible when a reference image exists.
                - Avoid the default centered hero plus simple card grid unless the reference image clearly uses that structure.
                - Do not invent fake sections or fake content.
                - Keep the output visually intentional and portfolio-specific rather than generic.
                - No markdown fences.
                - No explanations.
                - No JSON.
                - If a reference image is attached, treat it as the primary design template. The theme direction is secondary and should support the reference image instead of overriding it.

                Theme direction:
                {{themeDirection}}

                {{referenceImageGuidance}}

                Portfolio data:
                {{profileJson}}
                """;
    }

    private static string BuildReadme(UserProfileDto profile)
    {
        var fullName = profile.PersonalInfo?.FullName;
        var theme = string.IsNullOrWhiteSpace(profile.Theme) ? "Minimal" : profile.Theme;

        var builder = new StringBuilder();
        builder.AppendLine("# Generated Portfolio");
        builder.AppendLine();
        builder.AppendLine($"This package was created for {(string.IsNullOrWhiteSpace(fullName) ? "this portfolio owner" : fullName)} using the {theme} theme.");
        builder.AppendLine();
        builder.AppendLine("## Run the frontend");
        builder.AppendLine("1. Open the `MyPortfolioUI` folder.");
        builder.AppendLine("2. Run `npm install`.");
        builder.AppendLine("3. Run `npm run dev`.");
        builder.AppendLine();
        builder.AppendLine("## Run the backend");
        builder.AppendLine("1. Open the `MyPortfolioAPI` folder.");
        builder.AppendLine("2. No database config changes are needed. The generated API uses a local SQLite file by default.");
        builder.AppendLine("3. Run `dotnet restore`.");
        builder.AppendLine("4. Run `dotnet run`.");
        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine("- The frontend expects the backend to run on `http://localhost:5000`.");
        builder.AppendLine("- The backend stores one portfolio document in a local `portfolio.db` SQLite file through Entity Framework Core.");
        builder.AppendLine("- You can optionally override the frontend API host with `VITE_API_URL`.");
        builder.AppendLine("- You can update the generated portfolio through `PUT /api/portfolio`.");
        return builder.ToString();
    }

    private static string BuildThemeDirection(string? theme)
    {
        return theme switch
        {
            "Dark Pro" => """
                Create a premium dark interface with a sharp, high-end professional feel.
                Use deep dark surfaces, strong contrast, controlled highlights, precise spacing, and confident typography.
                Favor layered depth, premium panel treatment, and restrained accent use over playful effects.
                The result should feel like a serious personal brand site for a technically strong professional, not a gaming dashboard and not a generic SaaS page.
                If the reference image suggests a specific composition, density, or hero structure, preserve that structure closely while translating it into this dark premium visual language.
                """,
            "Creative" => """
                Create an expressive, high-personality interface with bold composition and memorable pacing.
                Use asymmetry, layered shapes, vivid but controlled accents, and more formal design confidence than a standard portfolio template.
                Keep it readable, editorial, and polished rather than chaotic or novelty-driven.
                If the reference image suggests a specific composition, density, or hero structure, preserve that structure closely while translating it into this expressive creative visual language.
                """,
            _ => """
                Create a refined minimalist interface with calm editorial spacing, elegant typography, bright surfaces, and disciplined restraint.
                Minimal does not mean empty or generic. It should still feel designed, premium, and compositionally confident.
                Use whitespace, alignment, proportion, and typography contrast as the main design tools instead of decorative effects.
                If the reference image suggests a specific composition, density, or hero structure, preserve that structure closely while translating it into this refined minimalist visual language.
                """
        };
    }

    private static string BuildReferenceImageGuidance(ReferenceImageDto? referenceImage)
    {
        if (referenceImage is null)
        {
            return "No reference image is attached. Create an original design from the profile data and theme direction alone.";
        }

        var notes = string.IsNullOrWhiteSpace(referenceImage.Notes)
            ? "No extra notes were supplied."
            : $"User notes about the reference image: {referenceImage.Notes.Trim()}";

        return $$"""
                A reference image is attached to this prompt.
                Treat the reference image as the primary template for the generated UI.
                Follow its overall composition closely.
                Prioritize matching:
                - section ordering
                - hero layout
                - alignment and spacing rhythm
                - column structure and content density
                - card shapes and grouping patterns
                - typography mood and scale relationships
                - color hierarchy and contrast strategy
                - the overall visual hierarchy from top to bottom
                - edge treatment, surface treatment, and framing language
                - how dense or airy each region of the page feels
                Aim for a result that feels very close to the reference image in structure and presentation, while still using the user's own portfolio content.
                The image should behave more like a layout and art-direction template than a loose inspiration board.
                Do not fall back to a generic centered hero plus simple card grid unless the reference image itself clearly uses that structure.
                Do not flatten a dense, editorial, asymmetric, or premium composition into a safer default layout.
                Preserve the emotional tone and pacing of the reference, not just its color palette.
                Do not copy logos, exact text, or assets from the image.
                Recreate the same design language and layout patterns in an original way using the portfolio's own content.
                {{notes}}
                """;
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

    [GeneratedRegex("""\bimport\s+(?:[^;]*?\s+from\s+)?["'](?<spec>[^"']+)["']""", RegexOptions.CultureInvariant)]
    private static partial Regex StaticImportPattern();

    [GeneratedRegex("""\bexport\s+[^;]*?\s+from\s+["'](?<spec>[^"']+)["']""", RegexOptions.CultureInvariant)]
    private static partial Regex ExportFromPattern();

    [GeneratedRegex("""\bimport\(\s*["'](?<spec>[^"']+)["']\s*\)""", RegexOptions.CultureInvariant)]
    private static partial Regex DynamicImportPattern();

    [GeneratedRegex("""\brequire\(\s*["'](?<spec>[^"']+)["']\s*\)""", RegexOptions.CultureInvariant)]
    private static partial Regex RequirePattern();
}
