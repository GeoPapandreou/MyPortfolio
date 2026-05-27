using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data.Configurations;

public sealed class PortfolioVersionConfiguration : IEntityTypeConfiguration<PortfolioVersion>
{
    public void Configure(EntityTypeBuilder<PortfolioVersion> entity)
    {
        entity.HasKey(version => version.Id);
        entity.Property(version => version.ZipUrl).HasMaxLength(512).IsRequired();
        entity.Property(version => version.VersionNumber).IsRequired();
        entity.Property(version => version.GeneratedAt).IsRequired();

        entity.HasOne(version => version.Portfolio)
            .WithMany(portfolio => portfolio.Versions)
            .HasForeignKey(version => version.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
