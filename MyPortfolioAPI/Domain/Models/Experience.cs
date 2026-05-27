namespace MyPortfolioAPI.Models;

public sealed class Experience
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }

    public string Organisation { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public Portfolio? Portfolio { get; set; }

    public List<ExperienceBullet> Bullets { get; set; } = new();
}
