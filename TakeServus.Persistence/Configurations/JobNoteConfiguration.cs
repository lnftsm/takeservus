namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

public class JobNoteConfiguration : IEntityTypeConfiguration<JobNote>
{
    public void Configure(EntityTypeBuilder<JobNote> builder)
    {
        builder.HasKey(jn => jn.Id);

        builder.Property(jn => jn.Note).IsRequired();
        
        builder.HasOne(jn => jn.Job).WithMany(j => j.Notes).HasForeignKey(jn => jn.JobId);
        builder.HasOne(jn => jn.CreatedByUser).WithMany().HasForeignKey("CreatedByUserId").OnDelete(DeleteBehavior.Restrict); 
    }
}