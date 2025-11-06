using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");
        
        builder.HasKey(a => a.AccountId);
        builder.Property(a => a.AccountId)
            .HasColumnName("account_id")
            .UseIdentityColumn();
        
        builder.Property(a => a.PlayerId)
            .HasColumnName("player_id")
            .IsRequired();
        
        builder.Property(a => a.CurrencyNetworkId)
            .HasColumnName("currency_network_id")
            .IsRequired();
        
        var accountTypeConverter = new ValueConverter<AccountType, string>(
            v => v switch
            {
                AccountType.HOUSE_WAGER => "HOUSE:WAGER",
                AccountType.HOUSE_FEES => "HOUSE:FEES",
                _ => v.ToString()
            },
            v => v switch
            {
                "HOUSE:WAGER" => AccountType.HOUSE_WAGER,
                "HOUSE:FEES" => AccountType.HOUSE_FEES,
                _ => Enum.Parse<AccountType>(v)
            });
        
        builder.Property(a => a.AccountType)
            .HasColumnName("account_type")
            .HasConversion(accountTypeConverter)
            .IsRequired();
        
        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("ACTIVE");
        
        builder.HasIndex(a => new { a.PlayerId, a.CurrencyNetworkId, a.AccountType })
            .IsUnique();
        
        builder.HasOne(a => a.CurrencyNetwork)
            .WithMany(cn => cn.Accounts)
            .HasForeignKey(a => a.CurrencyNetworkId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Check constraint for account_type
        var validAccountTypes = "'MAIN','WITHDRAW_HOLD','BONUS','HOUSE','HOUSE:WAGER','HOUSE:FEES'";
        builder.HasCheckConstraint("CK_accounts_account_type", $"account_type IN ({validAccountTypes})");
    }
}

