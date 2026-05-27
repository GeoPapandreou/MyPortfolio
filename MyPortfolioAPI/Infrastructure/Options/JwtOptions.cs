namespace MyPortfolioAPI.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public const string PlaceholderSecret = "replace-this-with-a-long-random-secret";
    public const string ExamplePlaceholderSecret = "replace-with-a-long-random-secret";

    public const int MinimumSecretLength = 32;

    public static readonly string[] KnownPlaceholderSecrets =
    [
        PlaceholderSecret,
        ExamplePlaceholderSecret,
        "your-super-secret-key-with-at-least-32-characters"
    ];

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "MyPortfolioAPI";

    public string Audience { get; set; } = "MyPortfolioUI";

    public int ExpiryMinutes { get; set; } = 720;

    public static bool HasConfiguredSecret(string? secret)
    {
        return !string.IsNullOrWhiteSpace(secret) &&
               !KnownPlaceholderSecrets.Any(placeholder => string.Equals(secret, placeholder, StringComparison.Ordinal));
    }
}
