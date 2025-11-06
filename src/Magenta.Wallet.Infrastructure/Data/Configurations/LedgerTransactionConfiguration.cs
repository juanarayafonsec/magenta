using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("ledger_transactions");
        
        builder.HasKey(lt => lt.TxId);
        builder.Property(lt => lt.TxId)
            .HasColumnName("tx_id");
        
        builder.Property(lt => lt.TxType)
            .HasColumnName("tx_type")
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(lt => lt.ExternalRef)
            .HasColumnName("external_ref")
            .HasMaxLength(255);
        
        builder.Property(lt => lt.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");
        
        builder.Property(lt => lt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");
        
        builder.HasIndex(lt => lt.CreatedAt);
    }
}




