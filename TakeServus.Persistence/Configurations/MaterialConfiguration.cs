namespace TakeServus.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TakeServus.Domain.Entities;

// Configuration for the Material entity
public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Unit).IsRequired().HasMaxLength(20);
        builder.Property(m => m.UnitPrice).HasPrecision(10, 2);
        builder.Property(m => m.StockQuantity).IsRequired();
    }
}
