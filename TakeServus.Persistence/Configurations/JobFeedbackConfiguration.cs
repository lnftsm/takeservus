using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

namespace TakeServus.Persistence.Configurations;

public class JobFeedbackConfiguration : IEntityTypeConfiguration<JobFeedback>
{
  public void Configure(EntityTypeBuilder<JobFeedback> builder)
  {
    builder.HasKey(f => f.Id);

    builder.Property(f => f.IsSatisfied)
        .IsRequired();

    builder.Property(f => f.Rating)
        .HasColumnType("int")
        .HasDefaultValue(null);

    builder.Property(f => f.Comment)
        .HasMaxLength(1000);

    builder.Property(f => f.SubmittedAt)
        .IsRequired();

    builder.HasIndex(f => new { f.JobId, f.CustomerId })
        .IsUnique();

    builder.HasOne(f => f.Job)
        .WithMany()
        .HasForeignKey(f => f.JobId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(f => f.Customer)
        .WithMany()
        .HasForeignKey(f => f.CustomerId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
