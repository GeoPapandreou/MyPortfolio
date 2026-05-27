using Microsoft.EntityFrameworkCore;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data.Queries;

public static class PortfolioQueryExtensions
{
    public static IQueryable<Portfolio> IncludeAggregate(this IQueryable<Portfolio> query)
    {
        return query
            .AsSplitQuery()
            .Include(portfolio => portfolio.User)
            .Include(portfolio => portfolio.PersonalInfo)
            .Include(portfolio => portfolio.ContactInfo)
            .Include(portfolio => portfolio.Experiences)
                .ThenInclude(experience => experience.Bullets)
            .Include(portfolio => portfolio.WorkSamples)
                .ThenInclude(sample => sample.Tools)
            .Include(portfolio => portfolio.Versions);
    }

    public static IQueryable<Portfolio> IncludePersistenceState(this IQueryable<Portfolio> query)
    {
        return query
            .AsSplitQuery()
            .Include(portfolio => portfolio.PersonalInfo)
            .Include(portfolio => portfolio.ContactInfo)
            .Include(portfolio => portfolio.Versions);
    }

    public static IQueryable<User> IncludeAccountAggregate(this IQueryable<User> query)
    {
        return query
            .AsSplitQuery()
            .Include(user => user.Portfolio!)
                .ThenInclude(portfolio => portfolio.PersonalInfo)
            .Include(user => user.Portfolio!)
                .ThenInclude(portfolio => portfolio.ContactInfo)
            .Include(user => user.Portfolio!)
                .ThenInclude(portfolio => portfolio.Versions);
    }
}
