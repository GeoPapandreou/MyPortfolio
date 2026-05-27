namespace MyPortfolioAPI.Options;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    public const string PlaceholderApiKey = "replace-with-your-gemini-api-key";
    public static readonly string[] KnownPlaceholderApiKeys = [PlaceholderApiKey, "your-gemini-api-key"];

    public string ApiKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent";

    public double Temperature { get; set; } = 0.2;

    public int MaxOutputTokens { get; set; } = 16384;

    public static bool HasConfiguredApiKey(string? apiKey)
    {
        return !string.IsNullOrWhiteSpace(apiKey) &&
               !KnownPlaceholderApiKeys.Any(placeholder => string.Equals(apiKey, placeholder, StringComparison.Ordinal));
    }
}
