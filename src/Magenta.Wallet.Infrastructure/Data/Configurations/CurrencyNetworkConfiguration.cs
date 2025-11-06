using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class CurrencyNetworkConfiguration : IEntityTypeConfiguration<CurrencyNetwork>
{
    public void Configure(EntityTypeBuilder<CurrencyNetwork> builder)
    {
        builder.ToTable("currency_networks");
        
        builder.HasKey(cn => cn.CurrencyNetworkId);
        builder.Property(cn => cn.CurrencyNetworkId)
            .HasColumnName("currency_network_id")
            .UseIdentityColumn();
        
        builder.Property(cn => cn.CurrencyId)
            .HasColumnName("currency_id")
            .IsRequired();
        
        builder.Property(cn => cn.NetworkId)
            .HasColumnName("network_id")
            .IsRequired();
        
        builder.Property(cn => cn.TokenContract)
            .HasColumnName("token_contract");
        
        builder.Property(cn => cn.WithdrawalFeeMinor)
            .HasColumnName("withdrawal_fee_minor")
            .HasDefaultValue(0L);
        
        builder.Property(cn => cn.MinDepositMinor)
            .HasColumnName("min_deposit_minor")
            .HasDefaultValue(0L);
        
        builder.Property(cn => cn.MinWithdrawMinor)
            .HasColumnName("min_withdraw_minor")
            .HasDefaultValue(0L);
        
        builder.Property(cn => cn.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
        
        builder.HasIndex(cn => new { cn.CurrencyId, cn.NetworkId })
            .IsUnique();
        
        builder.HasOne(cn => cn.Currency)
            .WithMany(c => c.CurrencyNetworks)
            .HasForeignKey(cn => cn.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(cn => cn.Network)
            .WithMany(n => n.CurrencyNetworks)
            .HasForeignKey(cn => cn.NetworkId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}




