using Microsoft.EntityFrameworkCore;
using MyPortfolioAPI.Data;
using MyPortfolioAPI.Data.Queries;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Services;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> LoadAccountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsForOtherUserAsync(Guid userId, string email, CancellationToken cancellationToken = default);

    void Add(User user);

    void Remove(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
    }

    public async Task<User?> LoadAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .IncludeAccountAggregate()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<bool> EmailExistsForOtherUserAsync(Guid userId, string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.AnyAsync(user => user.Id != userId && user.Email == email, cancellationToken);
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }

    public void Remove(User user)
    {
        _dbContext.Users.Remove(user);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
