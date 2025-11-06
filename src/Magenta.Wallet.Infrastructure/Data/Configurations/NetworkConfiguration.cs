using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class NetworkConfiguration : IEntityTypeConfiguration<Network>
{
    public void Configure(EntityTypeBuilder<Network> builder)
    {
        builder.ToTable("networks");
        
        builder.HasKey(n => n.NetworkId);
        builder.Property(n => n.NetworkId)
            .HasColumnName("network_id")
            .UseIdentityColumn();
        
        builder.Property(n => n.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(n => n.Name)
            .IsUnique();
        
        builder.Property(n => n.NativeSymbol)
            .HasColumnName("native_symbol")
            .IsRequired()
            .HasMaxLength(10);
        
        builder.Property(n => n.ConfirmationsRequired)
            .HasColumnName("confirmations_required")
            .HasDefaultValue(1);
        
        builder.Property(n => n.ExplorerUrl)
            .HasColumnName("explorer_url");
        
        builder.Property(n => n.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
    }
}




