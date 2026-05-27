using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data.Configurations;

public sealed class PortfolioConfiguration : IEntityTypeConfiguration<Portfolio>
{
    public void Configure(EntityTypeBuilder<Portfolio> entity)
    {
        entity.HasKey(portfolio => portfolio.Id);
        entity.Property(portfolio => portfolio.Theme).HasMaxLength(64).IsRequired();
        entity.HasIndex(portfolio => portfolio.UserId).IsUnique();

        entity.HasOne(portfolio => portfolio.User)
            .WithOne(user => user.Portfolio)
            .HasForeignKey<Portfolio>(portfolio => portfolio.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
