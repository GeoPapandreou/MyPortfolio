using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Extensions;

public static class PortfolioMappingExtensions
{
    public static UserProfileDto ToDto(this Portfolio? portfolio)
    {
        if (portfolio is null)
        {
            return CreateEmpty();
        }

        return new UserProfileDto
        {
            Theme = portfolio.Theme,
            PersonalInfo = new PersonalInfoDto
            {
                FullName = portfolio.PersonalInfo?.FullName ?? string.Empty,
                Profession = portfolio.PersonalInfo?.Profession ?? string.Empty,
                Bio = portfolio.PersonalInfo?.Bio ?? string.Empty,
                PhotoUrl = portfolio.PersonalInfo?.PhotoUrl ?? string.Empty,
                Location = portfolio.PersonalInfo?.Location ?? string.Empty
            },
            Experiences = portfolio.Experiences
                .OrderByDescending(item => item.StartDate)
                .Select(item => new ExperienceDto
                {
                    Organisation = item.Organisation,
                    Role = item.Role,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    IsCurrent = item.IsCurrent,
                    Bullets = item.Bullets.Select(bullet => bullet.Text).ToList()
                })
                .ToList(),
            WorkSamples = portfolio.WorkSamples
                .Select(item => new WorkSampleDto
                {
                    Title = item.Title,
                    Description = item.Description,
                    LiveUrl = item.LiveUrl,
                    Tools = item.Tools.Select(tool => tool.Tag).ToList()
                })
                .ToList(),
            ContactInfo = new ContactInfoDto
            {
                Email = portfolio.ContactInfo?.Email ?? string.Empty,
                Phone = portfolio.ContactInfo?.Phone ?? string.Empty,
                LinkedIn = portfolio.ContactInfo?.LinkedIn ?? string.Empty,
                Instagram = portfolio.ContactInfo?.Instagram ?? string.Empty,
                Facebook = portfolio.ContactInfo?.Facebook ?? string.Empty,
                GitHub = portfolio.ContactInfo?.GitHub ?? string.Empty
            },
            Versions = portfolio.Versions
                .OrderByDescending(item => item.GeneratedAt)
                .Select(item => new PortfolioVersionDto
                {
                    Id = item.Id,
                    GeneratedAt = item.GeneratedAt
                })
                .ToList()
        };
    }

    public static UserProfileDto CreateEmpty()
    {
        return new UserProfileDto
        {
            Theme = "Minimal",
            PersonalInfo = new PersonalInfoDto(),
            Experiences = new List<ExperienceDto>(),
            WorkSamples = new List<WorkSampleDto>(),
            ContactInfo = new ContactInfoDto(),
            Versions = new List<PortfolioVersionDto>()
        };
    }

}
