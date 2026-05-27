namespace MyPortfolioAPI.Models;

public sealed class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Profession { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Portfolio? Portfolio { get; set; }
}
