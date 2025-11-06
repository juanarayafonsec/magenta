using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class AccountBalanceConfiguration : IEntityTypeConfiguration<AccountBalance>
{
    public void Configure(EntityTypeBuilder<AccountBalance> builder)
    {
        builder.ToTable("account_balances");
        
        builder.HasKey(ab => ab.AccountId);
        builder.Property(ab => ab.AccountId)
            .HasColumnName("account_id");
        
        builder.Property(ab => ab.BalanceMinor)
            .HasColumnName("balance_minor")
            .HasDefaultValue(0L)
            .IsRequired();
        
        builder.Property(ab => ab.ReservedMinor)
            .HasColumnName("reserved_minor")
            .HasDefaultValue(0L)
            .IsRequired();
        
        builder.Property(ab => ab.CashableMinor)
            .HasColumnName("cashable_minor")
            .HasDefaultValue(0L)
            .IsRequired();
        
        builder.Property(ab => ab.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");
        
        builder.HasOne(ab => ab.Account)
            .WithOne(a => a.Balance)
            .HasForeignKey<AccountBalance>(ab => ab.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}




