using System.Text.Json;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Utilities;

namespace MyPortfolioAPI.Services;

public interface IPortfolioFrontendPromptBuilder
{
    string BuildPrimaryPrompt(UserProfileDto profile, ReferenceImageDto? referenceImage);

    string BuildRepairPrompt(UserProfileDto profile, ReferenceImageDto? referenceImage);

    string BuildDelimitedPrompt(UserProfileDto profile, ReferenceImageDto? referenceImage);
}

public sealed class PortfolioFrontendPromptBuilder : IPortfolioFrontendPromptBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string BuildPrimaryPrompt(UserProfileDto profile, ReferenceImageDto? referenceImage)
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

    public string BuildRepairPrompt(UserProfileDto profile, ReferenceImageDto? referenceImage)
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

    public string BuildDelimitedPrompt(UserProfileDto profile, ReferenceImageDto? referenceImage)
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
}
