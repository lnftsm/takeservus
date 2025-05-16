namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PhoneNumber).HasMaxLength(30);
        builder.Property(u => u.Role).HasMaxLength(30);
        builder.Property(u => u.PasswordHash).IsRequired();
        
        builder.HasOne(u => u.Technician).WithOne(t => t.User).HasForeignKey<Technician>(t => t.UserId);
    }
}