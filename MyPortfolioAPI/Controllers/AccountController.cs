using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPortfolioAPI.Data;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Extensions;

namespace MyPortfolioAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AppDbContext dbContext, IWebHostEnvironment environment, ILogger<AccountController> logger)
    {
        _dbContext = dbContext;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<AccountSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var account = await LoadAccountAsync(userId, cancellationToken);
        if (account is null)
        {
            return NotFound(new { message = "Your account could not be found." });
        }

        return Ok(ToDto(account));
    }

    [HttpPut]
    public async Task<ActionResult<AccountSettingsDto>> PutAsync(AccountSettingsDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var account = await LoadAccountAsync(userId, cancellationToken);
        if (account is null)
        {
            return NotFound(new { message = "Your account could not be found." });
        }

        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Full name and email address are required." });
        }

        var emailTaken = await _dbContext.Users
            .AnyAsync(user => user.Id != userId && user.Email == email, cancellationToken);
        if (emailTaken)
        {
            return Conflict(new { message = "Another account already uses this email address." });
        }

        account.FullName = fullName;
        account.Email = email;
        account.Profession = request.Profession?.Trim() ?? string.Empty;
        account.Location = request.Location?.Trim() ?? string.Empty;
        account.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(account));
    }

    [HttpDelete("versions/{versionId:guid}")]
    public async Task<ActionResult<AccountSettingsDto>> DeletePortfolioVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var account = await LoadAccountAsync(userId, cancellationToken);
        if (account?.Portfolio is null)
        {
            return NotFound(new { message = "No saved portfolio could be found for this account." });
        }

        var version = account.Portfolio.Versions.FirstOrDefault(item => item.Id == versionId);
        if (version is null)
        {
            return NotFound(new { message = "The selected portfolio package could not be found." });
        }

        _dbContext.PortfolioVersions.Remove(version);
        await _dbContext.SaveChangesAsync(cancellationToken);
        TryDeletePortfolioArtifactFiles(userId, version);

        var refreshed = await LoadAccountAsync(userId, cancellationToken);
        return Ok(ToDto(refreshed ?? account));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var account = await _dbContext.Users
            .Include(user => user.Portfolio)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (account is null)
        {
            return NotFound(new { message = "Your account could not be found." });
        }

        _dbContext.Users.Remove(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
        TryDeleteUserArtifacts(userId);

        return NoContent();
    }

    private void TryDeletePortfolioArtifactFiles(Guid userId, Models.PortfolioVersion version)
    {
        if (!TryResolveArtifactPaths(userId, version.ZipUrl, out var zipPath, out var manifestPath, out var artifactsDirectory))
        {
            _logger.LogWarning(
                "Skipped deleting portfolio artifact files for version {VersionId} because stored path '{ZipUrl}' is outside the expected artifact root for user {UserId}.",
                version.Id,
                version.ZipUrl,
                userId);
            return;
        }

        try
        {
            if (System.IO.File.Exists(zipPath))
            {
                System.IO.File.Delete(zipPath);
            }

            if (System.IO.File.Exists(manifestPath))
            {
                System.IO.File.Delete(manifestPath);
            }

            if (Directory.Exists(artifactsDirectory) &&
                !Directory.EnumerateFileSystemEntries(artifactsDirectory).Any())
            {
                Directory.Delete(artifactsDirectory);
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                exception,
                "Portfolio version {VersionId} was removed from the database, but its saved artifact files could not be deleted yet.",
                version.Id);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(
                exception,
                "Portfolio version {VersionId} was removed from the database, but its saved artifact files could not be deleted yet.",
                version.Id);
        }
    }

    private void TryDeleteUserArtifacts(Guid userId)
    {
        var artifactsRoot = GetUserArtifactsRoot(userId);
        if (!Directory.Exists(artifactsRoot))
        {
            return;
        }

        try
        {
            Directory.Delete(artifactsRoot, true);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                exception,
                "User {UserId} was deleted from the database, but saved portfolio artifacts could not be removed yet.",
                userId);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(
                exception,
                "User {UserId} was deleted from the database, but saved portfolio artifacts could not be removed yet.",
                userId);
        }
    }

    private bool TryResolveArtifactPaths(Guid userId, string zipUrl, out string zipPath, out string manifestPath, out string artifactsDirectory)
    {
        zipPath = string.Empty;
        manifestPath = string.Empty;
        artifactsDirectory = string.Empty;

        if (string.IsNullOrWhiteSpace(zipUrl))
        {
            return false;
        }

        var artifactsRoot = GetUserArtifactsRoot(userId);
        var resolvedZipPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, zipUrl));
        if (!IsPathWithinRoot(resolvedZipPath, artifactsRoot))
        {
            return false;
        }

        var resolvedArtifactsDirectory = Path.GetDirectoryName(resolvedZipPath);
        if (string.IsNullOrWhiteSpace(resolvedArtifactsDirectory) || !IsPathWithinRoot(resolvedArtifactsDirectory, artifactsRoot))
        {
            return false;
        }

        var resolvedManifestPath = Path.Combine(
            resolvedArtifactsDirectory,
            $"{Path.GetFileNameWithoutExtension(resolvedZipPath)}.manifest.json");

        if (!IsPathWithinRoot(resolvedManifestPath, artifactsRoot))
        {
            return false;
        }

        zipPath = resolvedZipPath;
        manifestPath = resolvedManifestPath;
        artifactsDirectory = resolvedArtifactsDirectory;
        return true;
    }

    private string GetUserArtifactsRoot(Guid userId)
    {
        return Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath,
            "GeneratedArtifacts",
            userId.ToString("N")));
    }

    private static bool IsPathWithinRoot(string candidatePath, string rootPath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var normalizedRoot = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(candidatePath, normalizedRoot, comparison) ||
               candidatePath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison);
    }

    private async Task<Models.User?> LoadAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .Include(user => user.Portfolio!)
                .ThenInclude(portfolio => portfolio.PersonalInfo)
            .Include(user => user.Portfolio!)
                .ThenInclude(portfolio => portfolio.ContactInfo)
            .Include(user => user.Portfolio!)
                .ThenInclude(portfolio => portfolio.Versions)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    private static AccountSettingsDto ToDto(Models.User user)
    {
        var portfolio = user.Portfolio;
        var versions = user.Portfolio?.Versions
            .OrderByDescending(item => item.GeneratedAt)
            .Select(item => new PortfolioVersionDto
            {
                Id = item.Id,
                GeneratedAt = item.GeneratedAt
            })
            .ToList() ?? new List<PortfolioVersionDto>();

        return new AccountSettingsDto
        {
            FullName = user.FullName,
            Profession = string.IsNullOrWhiteSpace(user.Profession) ? portfolio?.PersonalInfo?.Profession ?? string.Empty : user.Profession,
            Location = string.IsNullOrWhiteSpace(user.Location) ? portfolio?.PersonalInfo?.Location ?? string.Empty : user.Location,
            PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? portfolio?.ContactInfo?.Phone ?? string.Empty : user.PhoneNumber,
            Email = user.Email,
            Versions = versions
        };
    }
}
