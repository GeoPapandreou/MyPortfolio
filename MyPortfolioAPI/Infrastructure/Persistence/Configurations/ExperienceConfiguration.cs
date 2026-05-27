using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data.Configurations;

public sealed class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> entity)
    {
        entity.HasKey(experience => experience.Id);
        entity.Property(experience => experience.Organisation).HasMaxLength(160);
        entity.Property(experience => experience.Role).HasMaxLength(160);

        entity.HasOne(experience => experience.Portfolio)
            .WithMany(portfolio => portfolio.Experiences)
            .HasForeignKey(experience => experience.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
