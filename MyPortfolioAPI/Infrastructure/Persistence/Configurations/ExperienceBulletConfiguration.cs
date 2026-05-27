using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data.Configurations;

public sealed class ExperienceBulletConfiguration : IEntityTypeConfiguration<ExperienceBullet>
{
    public void Configure(EntityTypeBuilder<ExperienceBullet> entity)
    {
        entity.HasKey(bullet => bullet.Id);
        entity.Property(bullet => bullet.Text).HasMaxLength(512).IsRequired();

        entity.HasOne(bullet => bullet.Experience)
            .WithMany(experience => experience.Bullets)
            .HasForeignKey(bullet => bullet.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
