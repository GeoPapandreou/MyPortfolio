using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data.Configurations;

public sealed class PersonalInfoConfiguration : IEntityTypeConfiguration<PersonalInfo>
{
    public void Configure(EntityTypeBuilder<PersonalInfo> entity)
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
    }
}
