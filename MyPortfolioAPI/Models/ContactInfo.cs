namespace MyPortfolioAPI.Models;

public sealed class ContactInfo
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string LinkedIn { get; set; } = string.Empty;

    public string Instagram { get; set; } = string.Empty;

    public string Facebook { get; set; } = string.Empty;

    public string GitHub { get; set; } = string.Empty;

    public Portfolio? Portfolio { get; set; }
}
