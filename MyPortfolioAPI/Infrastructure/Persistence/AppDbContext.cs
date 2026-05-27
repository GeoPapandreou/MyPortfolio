using Microsoft.EntityFrameworkCore;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    public DbSet<PersonalInfo> PersonalInfos => Set<PersonalInfo>();

    public DbSet<Experience> Experiences => Set<Experience>();

    public DbSet<ExperienceBullet> ExperienceBullets => Set<ExperienceBullet>();

    public DbSet<WorkSample> WorkSamples => Set<WorkSample>();

    public DbSet<WorkSampleTool> WorkSampleTools => Set<WorkSampleTool>();

    public DbSet<ContactInfo> ContactInfos => Set<ContactInfo>();

    public DbSet<PortfolioVersion> PortfolioVersions => Set<PortfolioVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
