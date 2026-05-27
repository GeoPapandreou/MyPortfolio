namespace MyPortfolioAPI.Models;

public sealed class Portfolio
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Theme { get; set; } = "Minimal";

    public User? User { get; set; }

    public PersonalInfo? PersonalInfo { get; set; }

    public ContactInfo? ContactInfo { get; set; }

    public List<Experience> Experiences { get; set; } = new();

    public List<WorkSample> WorkSamples { get; set; } = new();

    public List<PortfolioVersion> Versions { get; set; } = new();
}
