namespace MyPortfolioAPI.Models;

public sealed class ExperienceBullet
{
    public Guid Id { get; set; }

    public Guid ExperienceId { get; set; }

    public string Text { get; set; } = string.Empty;

    public Experience? Experience { get; set; }
}
