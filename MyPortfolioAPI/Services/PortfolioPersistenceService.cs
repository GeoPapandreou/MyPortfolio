using Microsoft.EntityFrameworkCore;
using MyPortfolioAPI.Data;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Exceptions;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Services;

public interface IPortfolioPersistenceService
{
    Task<Portfolio?> LoadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Portfolio> SaveAsync(Guid userId, UserProfileDto profile, CancellationToken cancellationToken = default);
}

public sealed class PortfolioPersistenceService : IPortfolioPersistenceService
{
    private readonly AppDbContext _dbContext;

    public PortfolioPersistenceService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Portfolio?> LoadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Portfolios
            .Include(portfolio => portfolio.User)
            .Include(portfolio => portfolio.PersonalInfo)
            .Include(portfolio => portfolio.ContactInfo)
            .Include(portfolio => portfolio.Experiences)
                .ThenInclude(experience => experience.Bullets)
            .Include(portfolio => portfolio.WorkSamples)
                .ThenInclude(sample => sample.Tools)
            .Include(portfolio => portfolio.Versions)
            .FirstOrDefaultAsync(portfolio => portfolio.UserId == userId, cancellationToken);
    }

    public async Task<Portfolio> SaveAsync(Guid userId, UserProfileDto profile, CancellationToken cancellationToken = default)
    {
        _dbContext.ChangeTracker.Clear();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var portfolio = await _dbContext.Portfolios
            .Include(item => item.PersonalInfo)
            .Include(item => item.ContactInfo)
            .Include(item => item.Versions)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (portfolio is null)
        {
            portfolio = new Portfolio
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };

            _dbContext.Portfolios.Add(portfolio);
        }

        ApplyScalarValues(profile, portfolio);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await ReplaceChildCollectionsAsync(profile, portfolio, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _dbContext.ChangeTracker.Clear();
        return await LoadAsync(userId, cancellationToken)
            ?? throw new ClientSafeException("Your portfolio could not be saved right now. Please try again.", StatusCodes.Status500InternalServerError);
    }

    private async Task ReplaceChildCollectionsAsync(UserProfileDto profile, Portfolio portfolio, CancellationToken cancellationToken)
    {
        var experiences = profile.Experiences ?? new List<ExperienceDto>();
        var workSamples = profile.WorkSamples ?? new List<WorkSampleDto>();

        await _dbContext.Experiences
            .Where(item => item.PortfolioId == portfolio.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.WorkSamples
            .Where(item => item.PortfolioId == portfolio.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var experienceEntities = experiences
            .Where(item => item is not null && HasAnyValue(item.Organisation, item.Role, item.Bullets))
            .Select(experience =>
            {
                var entity = new Experience
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolio.Id,
                    Organisation = Normalize(experience.Organisation),
                    Role = Normalize(experience.Role),
                    StartDate = experience.StartDate,
                    EndDate = experience.IsCurrent ? null : experience.EndDate,
                    IsCurrent = experience.IsCurrent
                };

                entity.Bullets = (experience.Bullets ?? new List<string>())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => new ExperienceBullet
                    {
                        Id = Guid.NewGuid(),
                        ExperienceId = entity.Id,
                        Text = Normalize(item)
                    })
                    .ToList();

                return entity;
            })
            .ToList();

        var workSampleEntities = workSamples
            .Where(item => item is not null && HasAnyValue(item.Title, item.Description, item.Tools))
            .Select(sample =>
            {
                var entity = new WorkSample
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolio.Id,
                    Title = Normalize(sample.Title),
                    Description = Normalize(sample.Description),
                    LiveUrl = Normalize(sample.LiveUrl)
                };

                entity.Tools = (sample.Tools ?? new List<string>())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => new WorkSampleTool
                    {
                        Id = Guid.NewGuid(),
                        WorkSampleId = entity.Id,
                        Tag = Normalize(item)
                    })
                    .ToList();

                return entity;
            })
            .ToList();

        _dbContext.Experiences.AddRange(experienceEntities);
        _dbContext.WorkSamples.AddRange(workSampleEntities);
    }

    private static void ApplyScalarValues(UserProfileDto dto, Portfolio portfolio)
    {
        var personalInfo = dto.PersonalInfo ?? new PersonalInfoDto();
        var contactInfo = dto.ContactInfo ?? new ContactInfoDto();

        portfolio.Theme = NormalizeOrDefault(dto.Theme, "Minimal");

        portfolio.PersonalInfo ??= new PersonalInfo
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolio.Id
        };

        portfolio.PersonalInfo.FullName = Normalize(personalInfo.FullName);
        portfolio.PersonalInfo.Profession = Normalize(personalInfo.Profession);
        portfolio.PersonalInfo.Bio = Normalize(personalInfo.Bio);
        portfolio.PersonalInfo.PhotoUrl = Normalize(personalInfo.PhotoUrl);
        portfolio.PersonalInfo.Location = Normalize(personalInfo.Location);

        portfolio.ContactInfo ??= new ContactInfo
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolio.Id
        };

        portfolio.ContactInfo.Email = Normalize(contactInfo.Email);
        portfolio.ContactInfo.Phone = Normalize(contactInfo.Phone);
        portfolio.ContactInfo.LinkedIn = Normalize(contactInfo.LinkedIn);
        portfolio.ContactInfo.Instagram = Normalize(contactInfo.Instagram);
        portfolio.ContactInfo.Facebook = Normalize(contactInfo.Facebook);
        portfolio.ContactInfo.GitHub = Normalize(contactInfo.GitHub);
    }

    private static bool HasAnyValue(params object?[] values)
    {
        return values.Any(value => value switch
        {
            string text => !string.IsNullOrWhiteSpace(text),
            IEnumerable<string> items => items.Any(item => !string.IsNullOrWhiteSpace(item)),
            _ => value is not null
        });
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string NormalizeOrDefault(string? value, string fallback)
    {
        var normalized = Normalize(value);
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
