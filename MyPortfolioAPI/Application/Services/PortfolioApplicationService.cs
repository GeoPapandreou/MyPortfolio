using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Exceptions;
using MyPortfolioAPI.Extensions;
using MyPortfolioAPI.Utilities;

namespace MyPortfolioAPI.Services;

public interface IPortfolioApplicationService
{
    Task<UserProfileDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfileDto> SaveAsync(Guid userId, UserProfileDto request, CancellationToken cancellationToken = default);
}

public sealed class PortfolioApplicationService : IPortfolioApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPortfolioPersistenceService _portfolioPersistenceService;

    public PortfolioApplicationService(IUserRepository userRepository, IPortfolioPersistenceService portfolioPersistenceService)
    {
        _userRepository = userRepository;
        _portfolioPersistenceService = portfolioPersistenceService;
    }

    public async Task<UserProfileDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var portfolio = await _portfolioPersistenceService.LoadAsync(userId, cancellationToken);
        var user = portfolio?.User
            ?? await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new ClientSafeException("Your account could not be found.", StatusCodes.Status404NotFound);
        }

        return ApplyAccountDefaults(portfolio.ToDto(), user);
    }

    public async Task<UserProfileDto> SaveAsync(Guid userId, UserProfileDto request, CancellationToken cancellationToken = default)
    {
        var profileToPersist = UserProfileSanitizer.CreatePersistenceSafeCopy(request);
        await _portfolioPersistenceService.SaveAsync(userId, profileToPersist, cancellationToken);
        var refreshed = await _portfolioPersistenceService.LoadAsync(userId, cancellationToken);
        return refreshed.ToDto();
    }

    private static UserProfileDto ApplyAccountDefaults(UserProfileDto profile, Models.User user)
    {
        if (string.IsNullOrWhiteSpace(profile.PersonalInfo.FullName) && !string.IsNullOrWhiteSpace(user.FullName))
        {
            profile.PersonalInfo.FullName = user.FullName;
        }

        if (string.IsNullOrWhiteSpace(profile.PersonalInfo.Profession) && !string.IsNullOrWhiteSpace(user.Profession))
        {
            profile.PersonalInfo.Profession = user.Profession;
        }

        if (string.IsNullOrWhiteSpace(profile.PersonalInfo.Location) && !string.IsNullOrWhiteSpace(user.Location))
        {
            profile.PersonalInfo.Location = user.Location;
        }

        if (string.IsNullOrWhiteSpace(profile.ContactInfo.Email) && !string.IsNullOrWhiteSpace(user.Email))
        {
            profile.ContactInfo.Email = user.Email;
        }

        if (string.IsNullOrWhiteSpace(profile.ContactInfo.Phone) && !string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            profile.ContactInfo.Phone = user.PhoneNumber;
        }

        return profile;
    }
}
