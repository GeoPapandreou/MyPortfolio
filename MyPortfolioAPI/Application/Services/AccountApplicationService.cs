using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Exceptions;

namespace MyPortfolioAPI.Services;

public interface IAccountApplicationService
{
    Task<AccountSettingsDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AccountSettingsDto> UpdateAsync(Guid userId, AccountSettingsDto request, CancellationToken cancellationToken = default);

    Task<AccountSettingsDto> DeletePortfolioVersionAsync(Guid userId, Guid versionId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class AccountApplicationService : IAccountApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPortfolioVersionRepository _portfolioVersionRepository;
    private readonly IPortfolioArtifactStorage _artifactStorage;

    public AccountApplicationService(
        IUserRepository userRepository,
        IPortfolioVersionRepository portfolioVersionRepository,
        IPortfolioArtifactStorage artifactStorage)
    {
        _userRepository = userRepository;
        _portfolioVersionRepository = portfolioVersionRepository;
        _artifactStorage = artifactStorage;
    }

    public async Task<AccountSettingsDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await LoadAccountAsync(userId, cancellationToken);
        if (account is null)
        {
            throw new ClientSafeException("Your account could not be found.", StatusCodes.Status404NotFound);
        }

        return ToDto(account);
    }

    public async Task<AccountSettingsDto> UpdateAsync(Guid userId, AccountSettingsDto request, CancellationToken cancellationToken = default)
    {
        var account = await LoadAccountAsync(userId, cancellationToken);
        if (account is null)
        {
            throw new ClientSafeException("Your account could not be found.", StatusCodes.Status404NotFound);
        }

        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            throw new ClientSafeException("Full name and email address are required.", StatusCodes.Status400BadRequest);
        }

        var emailTaken = await _userRepository.EmailExistsForOtherUserAsync(userId, email, cancellationToken);
        if (emailTaken)
        {
            throw new ClientSafeException("Another account already uses this email address.", StatusCodes.Status409Conflict);
        }

        account.FullName = fullName;
        account.Email = email;
        account.Profession = request.Profession?.Trim() ?? string.Empty;
        account.Location = request.Location?.Trim() ?? string.Empty;
        account.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

        await _userRepository.SaveChangesAsync(cancellationToken);
        return ToDto(account);
    }

    public async Task<AccountSettingsDto> DeletePortfolioVersionAsync(Guid userId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var account = await LoadAccountAsync(userId, cancellationToken);
        if (account?.Portfolio is null)
        {
            throw new ClientSafeException("No saved portfolio could be found for this account.", StatusCodes.Status404NotFound);
        }

        var version = account.Portfolio.Versions.FirstOrDefault(item => item.Id == versionId);
        if (version is null)
        {
            throw new ClientSafeException("The selected portfolio package could not be found.", StatusCodes.Status404NotFound);
        }

        await _portfolioVersionRepository.RemoveAsync(version, cancellationToken);
        _artifactStorage.DeleteVersionArtifacts(userId, version.Id, version.ZipUrl);

        var refreshed = await LoadAccountAsync(userId, cancellationToken);
        return ToDto(refreshed ?? account);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await _userRepository.LoadAccountAsync(userId, cancellationToken);

        if (account is null)
        {
            throw new ClientSafeException("Your account could not be found.", StatusCodes.Status404NotFound);
        }

        _userRepository.Remove(account);
        await _userRepository.SaveChangesAsync(cancellationToken);
        _artifactStorage.DeleteUserArtifacts(userId);
    }

    private async Task<Models.User?> LoadAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.LoadAccountAsync(userId, cancellationToken);
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
