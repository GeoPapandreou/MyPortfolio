using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPortfolioAPI.Data;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Extensions;
using MyPortfolioAPI.Services;
using MyPortfolioAPI.Utilities;

namespace MyPortfolioAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolio")]
public sealed class PortfolioController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPortfolioPersistenceService _portfolioPersistenceService;

    public PortfolioController(AppDbContext dbContext, IPortfolioPersistenceService portfolioPersistenceService)
    {
        _dbContext = dbContext;
        _portfolioPersistenceService = portfolioPersistenceService;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var portfolio = await _portfolioPersistenceService.LoadAsync(userId, cancellationToken);

        // When a portfolio exists the User is loaded via the navigation property
        // included by LoadAsync, avoiding a second round-trip.
        // For new accounts that have no portfolio yet we fall back to a direct query.
        var user = portfolio?.User
            ?? await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "Your account could not be found." });
        }

        return Ok(ApplyAccountDefaults(portfolio.ToDto(), user));
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileDto>> PutAsync(UserProfileDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var profileToPersist = UserProfileSanitizer.CreatePersistenceSafeCopy(request);
        await _portfolioPersistenceService.SaveAsync(userId, profileToPersist, cancellationToken);
        var refreshed = await _portfolioPersistenceService.LoadAsync(userId, cancellationToken);
        return Ok(refreshed.ToDto());
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
