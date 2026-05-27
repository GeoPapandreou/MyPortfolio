namespace MyPortfolioAPI.Models;

public sealed class PersonalInfo
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Profession { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string PhotoUrl { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public Portfolio? Portfolio { get; set; }
}
