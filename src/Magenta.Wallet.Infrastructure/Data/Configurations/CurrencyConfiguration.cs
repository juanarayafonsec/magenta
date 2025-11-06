using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");
        
        builder.HasKey(c => c.CurrencyId);
        builder.Property(c => c.CurrencyId)
            .HasColumnName("currency_id")
            .UseIdentityColumn();
        
        builder.Property(c => c.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasMaxLength(10);
        
        builder.HasIndex(c => c.Code)
            .IsUnique();
        
        builder.Property(c => c.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(c => c.Decimals)
            .HasColumnName("decimals")
            .IsRequired();
        
        builder.HasCheckConstraint("CK_currencies_decimals", "decimals BETWEEN 0 AND 18");
        
        builder.Property(c => c.IconUrl)
            .HasColumnName("icon_url");
        
        builder.Property(c => c.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);
        
        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
    }
}




