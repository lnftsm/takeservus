namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

// Configuration for the Technician entity
public class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.CurrentLatitude).HasPrecision(9, 6);
        builder.Property(t => t.CurrentLongitude).HasPrecision(9, 6);
        builder.Property(t => t.IsAvailable).HasDefaultValue(true);
        builder.HasOne(t => t.User).WithOne(u => u.Technician).HasForeignKey<Technician>(t => t.UserId);
    }
}