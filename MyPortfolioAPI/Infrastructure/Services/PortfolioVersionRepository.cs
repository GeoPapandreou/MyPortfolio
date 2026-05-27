using MyPortfolioAPI.Data;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Services;

public interface IPortfolioVersionRepository
{
    Task AddAsync(PortfolioVersion version, CancellationToken cancellationToken = default);

    Task RemoveAsync(PortfolioVersion version, CancellationToken cancellationToken = default);
}

public sealed class PortfolioVersionRepository : IPortfolioVersionRepository
{
    private readonly AppDbContext _dbContext;

    public PortfolioVersionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PortfolioVersion version, CancellationToken cancellationToken = default)
    {
        _dbContext.ChangeTracker.Clear();
        _dbContext.PortfolioVersions.Add(version);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(PortfolioVersion version, CancellationToken cancellationToken = default)
    {
        _dbContext.PortfolioVersions.Remove(version);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
