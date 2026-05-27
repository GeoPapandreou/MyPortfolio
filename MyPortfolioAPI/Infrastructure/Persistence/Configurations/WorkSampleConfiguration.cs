using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Data.Configurations;

public sealed class WorkSampleConfiguration : IEntityTypeConfiguration<WorkSample>
{
    public void Configure(EntityTypeBuilder<WorkSample> entity)
    {
        entity.HasKey(sample => sample.Id);
        entity.Property(sample => sample.Title).HasMaxLength(160);
        entity.Property(sample => sample.LiveUrl).HasMaxLength(512);

        entity.HasOne(sample => sample.Portfolio)
            .WithMany(portfolio => portfolio.WorkSamples)
            .HasForeignKey(sample => sample.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
