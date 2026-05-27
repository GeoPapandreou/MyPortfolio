using System.ComponentModel.DataAnnotations;
using MyPortfolioAPI.Validation;

namespace MyPortfolioAPI.DTOs;

public sealed class AccountSettingsDto
{
    [Required]
    [StringLength(ValidationLimits.FullNameMaxLength)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(ValidationLimits.ProfessionMaxLength)]
    public string Profession { get; set; } = string.Empty;

    [StringLength(ValidationLimits.LocationMaxLength)]
    public string Location { get; set; } = string.Empty;

    [StringLength(ValidationLimits.PhoneMaxLength)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(ValidationLimits.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    public List<PortfolioVersionDto> Versions { get; set; } = new();
}
