using System.ComponentModel.DataAnnotations;
using MyPortfolioAPI.Validation;

namespace MyPortfolioAPI.DTOs;

public sealed class GenerateRequestDto
{
    [Required]
    public UserProfileDto Profile { get; set; } = new();

    public ReferenceImageDto? ReferenceImage { get; set; }
}

public sealed class ReferenceImageDto : IValidatableObject
{
    [Required]
    [StringLength(64)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    public string Base64Data { get; set; } = string.Empty;

    [StringLength(ValidationLimits.ReferenceImageFileNameMaxLength)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(ValidationLimits.ReferenceImageNotesMaxLength)]
    public string Notes { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ValidationLimits.AllowedReferenceImageMimeTypes.Contains(MimeType, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                $"Reference images must be one of: {string.Join(", ", ValidationLimits.AllowedReferenceImageMimeTypes)}.",
                [nameof(MimeType)]);
        }

        if (string.IsNullOrWhiteSpace(Base64Data))
        {
            yield return new ValidationResult("Reference image data is required.", [nameof(Base64Data)]);
            yield break;
        }

        byte[] imageBytes;
        var invalidBase64 = false;
        try
        {
            imageBytes = Convert.FromBase64String(Base64Data);
        }
        catch (FormatException)
        {
            imageBytes = Array.Empty<byte>();
            invalidBase64 = true;
        }

        if (invalidBase64)
        {
            yield return new ValidationResult("Reference image data must be valid Base64.", [nameof(Base64Data)]);
            yield break;
        }

        if (imageBytes.Length == 0)
        {
            yield return new ValidationResult("Reference image data cannot be empty.", [nameof(Base64Data)]);
        }
        else if (imageBytes.Length > ValidationLimits.ReferenceImageMaxBytes)
        {
            yield return new ValidationResult(
                $"Reference images must be smaller than {ValidationLimits.ReferenceImageMaxBytes / (1024 * 1024)} MB.",
                [nameof(Base64Data)]);
        }
    }
}

public sealed class GeneratedPortfolioManifest
{
    public Dictionary<string, string> FrontendFiles { get; set; } = new();

    public Dictionary<string, string> BackendFiles { get; set; } = new();

    public string ReadmeContent { get; set; } = string.Empty;
}

public sealed class GeneratedPortfolioResult
{
    public GeneratedPortfolioManifest Manifest { get; set; } = new();

    public byte[] ZipBytes { get; set; } = Array.Empty<byte>();
}
