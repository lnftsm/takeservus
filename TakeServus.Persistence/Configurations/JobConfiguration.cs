namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

// Configuration for the Job entity
public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Title).HasMaxLength(200).IsRequired();
        builder.Property(j => j.Status).HasMaxLength(50).IsRequired();
        builder.Property(j => j.Description).HasMaxLength(1000);
        builder.Property(j => j.ScheduledAt).IsRequired();
        builder.Property(j => j.CreatedAt).IsRequired();
        builder.Property(j => j.IsDeleted).HasDefaultValue(false);
        builder.HasOne(j => j.Customer).WithMany(c => c.Jobs).HasForeignKey(j => j.CustomerId);
        builder.HasOne(j => j.Technician).WithMany(t => t.Jobs).HasForeignKey(j => j.TechnicianId);
        builder.HasMany(j => j.Activities).WithOne(a => a.Job).HasForeignKey(a => a.JobId);

    }
}