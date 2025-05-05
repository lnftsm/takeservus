using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

namespace TakeServus.Persistence.Configurations;

public class JobActivityConfiguration : IEntityTypeConfiguration<JobActivity>
{
    public void Configure(EntityTypeBuilder<JobActivity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActivityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Details)
            .HasMaxLength(2000);

        builder.Property(a => a.PerformedAt)
            .IsRequired();

        builder.HasOne(a => a.Job)
            .WithMany(j => j.Activities)
            .HasForeignKey(a => a.JobId);

        builder.HasOne(a => a.PerformedByUser)
            .WithMany()
            .HasForeignKey(a => a.PerformedByUserId);
    }
}