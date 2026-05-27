using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyPortfolioAPI.Models;
using MyPortfolioAPI.Validation;

namespace MyPortfolioAPI.Data.Configurations;

public sealed class WorkSampleToolConfiguration : IEntityTypeConfiguration<WorkSampleTool>
{
    public void Configure(EntityTypeBuilder<WorkSampleTool> entity)
    {
        entity.HasKey(tool => tool.Id);
        entity.Property(tool => tool.Tag).HasMaxLength(ValidationLimits.WorkSampleToolMaxLength).IsRequired();

        entity.HasOne(tool => tool.WorkSample)
            .WithMany(sample => sample.Tools)
            .HasForeignKey(tool => tool.WorkSampleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
