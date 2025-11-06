using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");
        
        builder.HasKey(ik => new { ik.Source, ik.IdempotencyKeyValue });
        
        builder.Property(ik => ik.Source)
            .HasColumnName("source")
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(ik => ik.IdempotencyKeyValue)
            .HasColumnName("idempotency_key")
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(ik => ik.TxId)
            .HasColumnName("tx_id")
            .IsRequired();
        
        builder.Property(ik => ik.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");
        
        builder.HasOne<LedgerTransaction>()
            .WithMany()
            .HasForeignKey(ik => ik.TxId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}




