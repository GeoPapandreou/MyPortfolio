using System.ComponentModel.DataAnnotations;
using MyPortfolioAPI.Validation;

namespace MyPortfolioAPI.DTOs;

public sealed class RegisterDto
{
    [Required]
    [StringLength(ValidationLimits.FullNameMaxLength)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(ValidationLimits.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(ValidationLimits.PasswordMaxLength, MinimumLength = ValidationLimits.MinRegistrationPasswordLength)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginDto
{
    [Required]
    [EmailAddress]
    [StringLength(ValidationLimits.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(ValidationLimits.PasswordMaxLength)]
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
