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
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.Profession).HasMaxLength(160);
            entity.Property(user => user.Location).HasMaxLength(160);
            entity.Property(user => user.PhoneNumber).HasMaxLength(64);
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.CreatedAt).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(portfolio => portfolio.Id);
            entity.Property(portfolio => portfolio.Theme).HasMaxLength(64).IsRequired();
            entity.HasIndex(portfolio => portfolio.UserId).IsUnique();

            entity.HasOne(portfolio => portfolio.User)
                .WithOne(user => user.Portfolio)
                .HasForeignKey<Portfolio>(portfolio => portfolio.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalInfo>(entity =>
        {
            entity.HasKey(info => info.Id);
            entity.Property(info => info.FullName).HasMaxLength(160);
            entity.Property(info => info.Profession).HasMaxLength(160);
            entity.Property(info => info.Location).HasMaxLength(160);
            entity.Property(info => info.PhotoUrl).HasMaxLength(512);
            entity.HasIndex(info => info.PortfolioId).IsUnique();

            entity.HasOne(info => info.Portfolio)
                .WithOne(portfolio => portfolio.PersonalInfo)
                .HasForeignKey<PersonalInfo>(info => info.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Experience>(entity =>
        {
            entity.HasKey(experience => experience.Id);
            entity.Property(experience => experience.Organisation).HasMaxLength(160);
            entity.Property(experience => experience.Role).HasMaxLength(160);

            entity.HasOne(experience => experience.Portfolio)
                .WithMany(portfolio => portfolio.Experiences)
                .HasForeignKey(experience => experience.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExperienceBullet>(entity =>
        {
            entity.HasKey(bullet => bullet.Id);
            entity.Property(bullet => bullet.Text).HasMaxLength(512).IsRequired();

            entity.HasOne(bullet => bullet.Experience)
                .WithMany(experience => experience.Bullets)
                .HasForeignKey(bullet => bullet.ExperienceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkSample>(entity =>
        {
            entity.HasKey(sample => sample.Id);
            entity.Property(sample => sample.Title).HasMaxLength(160);
            entity.Property(sample => sample.LiveUrl).HasMaxLength(512);

            entity.HasOne(sample => sample.Portfolio)
                .WithMany(portfolio => portfolio.WorkSamples)
                .HasForeignKey(sample => sample.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkSampleTool>(entity =>
        {
            entity.HasKey(tool => tool.Id);
            entity.Property(tool => tool.Tag).HasMaxLength(Validation.ValidationLimits.WorkSampleToolMaxLength).IsRequired();

            entity.HasOne(tool => tool.WorkSample)
                .WithMany(sample => sample.Tools)
                .HasForeignKey(tool => tool.WorkSampleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactInfo>(entity =>
        {
            entity.HasKey(contact => contact.Id);
            entity.Property(contact => contact.Email).HasMaxLength(256);
            entity.Property(contact => contact.Phone).HasMaxLength(64);
            entity.Property(contact => contact.LinkedIn).HasMaxLength(512);
            entity.Property(contact => contact.Instagram).HasMaxLength(512);
            entity.Property(contact => contact.Facebook).HasMaxLength(512);
            entity.Property(contact => contact.GitHub).HasMaxLength(512);
            entity.HasIndex(contact => contact.PortfolioId).IsUnique();

            entity.HasOne(contact => contact.Portfolio)
                .WithOne(portfolio => portfolio.ContactInfo)
                .HasForeignKey<ContactInfo>(contact => contact.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PortfolioVersion>(entity =>
        {
            entity.HasKey(version => version.Id);
            entity.Property(version => version.ZipUrl).HasMaxLength(512).IsRequired();
            entity.Property(version => version.VersionNumber).IsRequired();
            entity.Property(version => version.GeneratedAt).IsRequired();

            entity.HasOne(version => version.Portfolio)
                .WithMany(portfolio => portfolio.Versions)
                .HasForeignKey(version => version.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
