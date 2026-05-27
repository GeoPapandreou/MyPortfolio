using System.Text;
using MyPortfolioAPI.DTOs;

namespace MyPortfolioAPI.Services;

public interface IGeneratedPortfolioReadmeBuilder
{
    string Build(UserProfileDto profile);
}

public sealed class GeneratedPortfolioReadmeBuilder : IGeneratedPortfolioReadmeBuilder
{
    public string Build(UserProfileDto profile)
    {
        var fullName = profile.PersonalInfo?.FullName;
        var theme = string.IsNullOrWhiteSpace(profile.Theme) ? "Minimal" : profile.Theme;

        var builder = new StringBuilder();
        builder.AppendLine("# Generated Portfolio");
        builder.AppendLine();
        builder.AppendLine($"This package was created for {(string.IsNullOrWhiteSpace(fullName) ? "this portfolio owner" : fullName)} using the {theme} theme.");
        builder.AppendLine();
        builder.AppendLine("## Run the frontend");
        builder.AppendLine("1. Open the `MyPortfolioUI` folder.");
        builder.AppendLine("2. Run `npm install`.");
        builder.AppendLine("3. Run `npm run dev`.");
        builder.AppendLine();
        builder.AppendLine("## Run the backend");
        builder.AppendLine("1. Open the `MyPortfolioAPI` folder.");
        builder.AppendLine("2. No database config changes are needed. The generated API uses a local SQLite file by default.");
        builder.AppendLine("3. Run `dotnet restore`.");
        builder.AppendLine("4. Run `dotnet run`.");
        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine("- The frontend expects the backend to run on `http://localhost:5000`.");
        builder.AppendLine("- The backend stores one portfolio document in a local `portfolio.db` SQLite file through Entity Framework Core.");
        builder.AppendLine("- You can optionally override the frontend API host with `VITE_API_URL`.");
        builder.AppendLine("- You can update the generated portfolio through `PUT /api/portfolio`.");
        return builder.ToString();
    }
}
