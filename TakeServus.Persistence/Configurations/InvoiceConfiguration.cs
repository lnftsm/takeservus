namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

// Configuration for the Invoice entity
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        
        builder.Property(i => i.Amount).HasColumnType("decimal(10,2)").IsRequired();
        builder.Property(i => i.IsPaid).IsRequired();

        builder.HasOne(i => i.Job).WithOne(j => j.Invoice).HasForeignKey<Invoice>(i => i.JobId).OnDelete(DeleteBehavior.Cascade); 
    }
}