using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class InboxEventConfiguration : IEntityTypeConfiguration<InboxEvent>
{
    public void Configure(EntityTypeBuilder<InboxEvent> builder)
    {
        builder.ToTable("inbox_events");
        
        builder.HasKey(ie => ie.Id);
        builder.Property(ie => ie.Id)
            .HasColumnName("id")
            .UseIdentityColumn();
        
        builder.Property(ie => ie.Source)
            .HasColumnName("source")
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(ie => ie.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(ie => ie.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.Property(ie => ie.ProcessedAt)
            .HasColumnName("processed_at");
        
        builder.HasIndex(ie => new { ie.Source, ie.IdempotencyKey })
            .IsUnique();
    }
}




