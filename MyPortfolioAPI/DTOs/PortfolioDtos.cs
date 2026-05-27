using System.ComponentModel.DataAnnotations;
using MyPortfolioAPI.Validation;

namespace MyPortfolioAPI.DTOs;

public sealed class UserProfileDto : IValidatableObject
{
    [Required]
    [StringLength(ValidationLimits.ThemeMaxLength)]
    public string Theme { get; set; } = "Minimal";

    [Required]
    public PersonalInfoDto PersonalInfo { get; set; } = new();

    [Required]
    public List<ExperienceDto> Experiences { get; set; } = new();

    [Required]
    public List<WorkSampleDto> WorkSamples { get; set; } = new();

    [Required]
    public ContactInfoDto ContactInfo { get; set; } = new();

    public List<PortfolioVersionDto> Versions { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ValidationLimits.AllowedThemes.Contains(Theme, StringComparer.Ordinal))
        {
            yield return new ValidationResult(
                $"Theme must be one of: {string.Join(", ", ValidationLimits.AllowedThemes)}.",
                [nameof(Theme)]);
        }
    }
}

public sealed class PersonalInfoDto
{
    [StringLength(ValidationLimits.FullNameMaxLength)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(ValidationLimits.ProfessionMaxLength)]
    public string Profession { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    [StringLength(ValidationLimits.PhotoUrlMaxLength)]
    public string PhotoUrl { get; set; } = string.Empty;

    [StringLength(ValidationLimits.LocationMaxLength)]
    public string Location { get; set; } = string.Empty;
}

public sealed class ExperienceDto : IValidatableObject
{
    [StringLength(ValidationLimits.OrganisationMaxLength)]
    public string Organisation { get; set; } = string.Empty;

    [StringLength(ValidationLimits.RoleMaxLength)]
    public string Role { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public List<string> Bullets { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
        {
            yield return new ValidationResult(
                "End date cannot be earlier than start date.",
                [nameof(StartDate), nameof(EndDate)]);
        }

        for (var index = 0; index < Bullets.Count; index++)
        {
            var bullet = Bullets[index];
            if (!string.IsNullOrWhiteSpace(bullet) && bullet.Length > ValidationLimits.ExperienceBulletMaxLength)
            {
                yield return new ValidationResult(
                    $"Bullet points cannot be longer than {ValidationLimits.ExperienceBulletMaxLength} characters.",
                    [$"{nameof(Bullets)}[{index}]"]);
            }
        }
    }
}

public sealed class WorkSampleDto : IValidatableObject
{
    [StringLength(ValidationLimits.WorkSampleTitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> Tools { get; set; } = new();

    [StringLength(ValidationLimits.UrlMaxLength)]
    public string LiveUrl { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var index = 0; index < Tools.Count; index++)
        {
            var tool = Tools[index];
            if (!string.IsNullOrWhiteSpace(tool) && tool.Length > ValidationLimits.WorkSampleToolMaxLength)
            {
                yield return new ValidationResult(
                    $"Tool names cannot be longer than {ValidationLimits.WorkSampleToolMaxLength} characters.",
                    [$"{nameof(Tools)}[{index}]"]);
            }
        }
    }
}

public sealed class ContactInfoDto
{
    [EmailAddress]
    [StringLength(ValidationLimits.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [StringLength(ValidationLimits.PhoneMaxLength)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(ValidationLimits.UrlMaxLength)]
    public string LinkedIn { get; set; } = string.Empty;

    [StringLength(ValidationLimits.UrlMaxLength)]
    public string Instagram { get; set; } = string.Empty;

    [StringLength(ValidationLimits.UrlMaxLength)]
    public string Facebook { get; set; } = string.Empty;

    [StringLength(ValidationLimits.UrlMaxLength)]
    public string GitHub { get; set; } = string.Empty;
}

public sealed class PortfolioVersionDto
{
    public Guid Id { get; set; }

    public DateTime GeneratedAt { get; set; }
}
