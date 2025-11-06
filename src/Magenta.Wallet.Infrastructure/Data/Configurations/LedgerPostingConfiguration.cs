using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class LedgerPostingConfiguration : IEntityTypeConfiguration<LedgerPosting>
{
    public void Configure(EntityTypeBuilder<LedgerPosting> builder)
    {
        builder.ToTable("ledger_postings");
        
        builder.HasKey(lp => lp.PostingId);
        builder.Property(lp => lp.PostingId)
            .HasColumnName("posting_id")
            .UseIdentityColumn();
        
        builder.Property(lp => lp.TxId)
            .HasColumnName("tx_id")
            .IsRequired();
        
        builder.Property(lp => lp.AccountId)
            .HasColumnName("account_id")
            .IsRequired();
        
        var directionConverter = new ValueConverter<Direction, string>(
            v => v.ToString(),
            v => Enum.Parse<Direction>(v));
        
        builder.Property(lp => lp.Direction)
            .HasColumnName("direction")
            .HasConversion(directionConverter)
            .IsRequired();
        
        builder.Property(lp => lp.AmountMinor)
            .HasColumnName("amount_minor")
            .IsRequired();
        
        builder.HasCheckConstraint("CK_ledger_postings_amount_minor", "amount_minor >= 0");
        
        builder.Property(lp => lp.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");
        
        builder.HasOne(lp => lp.Transaction)
            .WithMany(lt => lt.Postings)
            .HasForeignKey(lp => lp.TxId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(lp => lp.Account)
            .WithMany(a => a.Postings)
            .HasForeignKey(lp => lp.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(lp => lp.TxId);
        builder.HasIndex(lp => lp.AccountId);
        builder.HasIndex(lp => lp.CreatedAt);
        
        // Check constraint for direction
        builder.HasCheckConstraint("CK_ledger_postings_direction", "direction IN ('DEBIT','CREDIT')");
    }
}




