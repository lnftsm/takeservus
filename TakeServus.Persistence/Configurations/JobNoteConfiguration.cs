namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

// Configuration for the JobNote entity
public class JobNoteConfiguration : IEntityTypeConfiguration<JobNote>
{
    public void Configure(EntityTypeBuilder<JobNote> builder)
    {
        builder.HasKey(jn => jn.Id);
        builder.Property(jn => jn.Note).IsRequired();
        builder.Property(jn => jn.CreatedAt).IsRequired();
        builder.HasOne(jn => jn.Job).WithMany(j => j.Notes).HasForeignKey(jn => jn.JobId);
    }
}