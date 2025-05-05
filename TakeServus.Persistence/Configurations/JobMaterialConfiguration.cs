namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

// Configuration for the JobMaterial entity
public class JobMaterialConfiguration : IEntityTypeConfiguration<JobMaterial>
{
    public void Configure(EntityTypeBuilder<JobMaterial> builder)
    {
        builder.HasKey(jm => jm.Id);
        builder.Property(jm => jm.QuantityUsed).IsRequired();
        builder.HasOne(jm => jm.Job).WithMany(j => j.JobMaterials).HasForeignKey(jm => jm.JobId);
        builder.HasOne(jm => jm.Material).WithMany(m => m.JobMaterials).HasForeignKey(jm => jm.MaterialId);
    }
}