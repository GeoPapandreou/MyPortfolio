using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data.Configurations;

public sealed class ContactInfoConfiguration : IEntityTypeConfiguration<ContactInfo>
{
    public void Configure(EntityTypeBuilder<ContactInfo> entity)
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
    }
}
