using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

namespace TakeServus.Persistence.Configurations;

public class QueuedEmailConfiguration : IEntityTypeConfiguration<QueuedEmail>
{
  public void Configure(EntityTypeBuilder<QueuedEmail> builder)
  {
    builder.HasKey(e => e.Id);

    builder.Property(e => e.To)
        .IsRequired()
        .HasMaxLength(255);

    builder.Property(e => e.Subject)
        .IsRequired()
        .HasMaxLength(255);

    builder.Property(e => e.Body)
        .IsRequired();

    builder.Property(e => e.CreatedAt)
        .IsRequired();

    builder.Property(e => e.IsSent)
        .IsRequired();

    builder.Property(e => e.RetryCount)
        .HasDefaultValue(0);
  }
}