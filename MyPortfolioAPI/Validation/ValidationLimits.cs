namespace MyPortfolioAPI.Validation;

public static class ValidationLimits
{
    public const int ThemeMaxLength = 64;
    public const int FullNameMaxLength = 160;
    public const int ProfessionMaxLength = 160;
    public const int LocationMaxLength = 160;
    public const int EmailMaxLength = 256;
    public const int PasswordMaxLength = 512;
    public const int PhoneMaxLength = 64;
    public const int PhotoUrlMaxLength = 512;
    public const int UrlMaxLength = 512;
    public const int ExperienceBulletMaxLength = 512;
    public const int WorkSampleTitleMaxLength = 160;
    public const int WorkSampleToolMaxLength = 80;
    public const int OrganisationMaxLength = 160;
    public const int RoleMaxLength = 160;
    public const int MinRegistrationPasswordLength = 8;
    public const int ReferenceImageMaxBytes = 4 * 1024 * 1024;
    public const int ReferenceImageNotesMaxLength = 1000;
    public const int ReferenceImageFileNameMaxLength = 255;

    public static readonly string[] AllowedThemes = ["Minimal", "Dark Pro", "Creative"];
    public static readonly string[] AllowedReferenceImageMimeTypes = ["image/png", "image/jpeg", "image/webp"];
}
