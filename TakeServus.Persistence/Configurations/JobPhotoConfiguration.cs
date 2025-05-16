namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;


public class JobPhotoConfiguration : IEntityTypeConfiguration<JobPhoto>
{
    public void Configure(EntityTypeBuilder<JobPhoto> builder)
    {
        builder.HasKey(jp => jp.Id);

        builder.Property(jp => jp.PhotoUrl).IsRequired();
        builder.Property(jp => jp.UploadedAt).IsRequired();
        
        builder.HasOne(jp => jp.Job).WithMany(j => j.Photos).HasForeignKey(jp => jp.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}