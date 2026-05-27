using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
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
    }
}
