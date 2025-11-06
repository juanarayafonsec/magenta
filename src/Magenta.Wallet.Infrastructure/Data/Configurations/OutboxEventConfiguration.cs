using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magenta.Wallet.Infrastructure.Data.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("outbox_events");
        
        builder.HasKey(oe => oe.Id);
        builder.Property(oe => oe.Id)
            .HasColumnName("id")
            .UseIdentityColumn();
        
        builder.Property(oe => oe.EventType)
            .HasColumnName("event_type")
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(oe => oe.RoutingKey)
            .HasColumnName("routing_key")
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(oe => oe.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.Property(oe => oe.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");
        
        builder.Property(oe => oe.PublishedAt)
            .HasColumnName("published_at");
        
        builder.HasIndex(oe => new { oe.PublishedAt, oe.CreatedAt })
            .HasFilter("[published_at] IS NULL");
    }
}




