namespace MyPortfolioAPI.Models;

public sealed class PortfolioVersion
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }

    public int VersionNumber { get; set; }

    public DateTime GeneratedAt { get; set; }

    public string ZipUrl { get; set; } = string.Empty;

    public Portfolio? Portfolio { get; set; }
}
