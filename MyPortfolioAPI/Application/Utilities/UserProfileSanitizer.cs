using System.Text.Json;
using MyPortfolioAPI.DTOs;

namespace MyPortfolioAPI.Utilities;

public static class UserProfileSanitizer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static UserProfileDto CreatePersistenceSafeCopy(UserProfileDto profile)
    {
        var clone = JsonSerializer.Deserialize<UserProfileDto>(
                        JsonSerializer.Serialize(profile, SerializerOptions),
                        SerializerOptions)
                    ?? new UserProfileDto();

        if (clone.PersonalInfo is not null &&
            LooksLikeInlineImageDataUrl(clone.PersonalInfo.PhotoUrl))
        {
            clone.PersonalInfo.PhotoUrl = string.Empty;
        }

        return clone;
    }

    private static bool LooksLikeInlineImageDataUrl(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
    }
}
