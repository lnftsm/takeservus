namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title).HasMaxLength(200).IsRequired();
        builder.Property(j => j.Status).HasMaxLength(50).IsRequired();
        builder.Property(j => j.Description).HasMaxLength(1000);

        builder.HasOne(j => j.Customer).WithMany(c => c.Jobs).HasForeignKey(j => j.CustomerId);
        builder.HasOne(j => j.Technician).WithMany(t => t.Jobs).HasForeignKey(j => j.TechnicianId);
        builder.HasMany(j => j.Activities).WithOne(a => a.Job).HasForeignKey(a => a.JobId);
        builder.HasMany(j => j.JobFeedbacks).WithOne(f => f.Job).HasForeignKey(f => f.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}