using Magenta.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Magenta.Payments.Infrastructure.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentProvider> PaymentProviders { get; set; }
    public DbSet<DepositSession> DepositSessions { get; set; }
    public DbSet<DepositRequest> DepositRequests { get; set; }
    public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<InboxEvent> InboxEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Payment Providers
        modelBuilder.Entity<PaymentProvider>(entity =>
        {
            entity.ToTable("payment_providers");
            entity.HasKey(e => e.ProviderId);
            entity.Property(e => e.ProviderId).HasColumnName("provider_id").UseIdentityColumn();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.ApiBaseUrl).HasColumnName("api_base_url");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        });

        // Deposit Sessions
        modelBuilder.Entity<DepositSession>(entity =>
        {
            entity.ToTable("deposit_sessions");
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.PlayerId).HasColumnName("player_id").IsRequired();
            entity.Property(e => e.ProviderId).HasColumnName("provider_id").IsRequired();
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id").IsRequired();
            entity.Property(e => e.Address).HasColumnName("address").IsRequired();
            entity.Property(e => e.MemoOrTag).HasColumnName("memo_or_tag");
            entity.Property(e => e.ProviderReference).HasColumnName("provider_reference");
            entity.Property(e => e.ExpectedAmountMinor).HasColumnName("expected_amount_minor");
            entity.Property(e => e.MinAmountMinor).HasColumnName("min_amount_minor");
            entity.Property(e => e.ConfirmationsRequired).HasColumnName("confirmations_required").HasDefaultValue(1);
            entity.Property(e => e.Status).HasColumnName("status").IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb")
                .HasConversion(
                    v => v.ToJsonString(),
                    v => JsonDocument.Parse(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasOne<PaymentProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Restrict);
        });

        // Deposit Requests
        modelBuilder.Entity<DepositRequest>(entity =>
        {
            entity.ToTable("deposit_requests");
            entity.HasKey(e => e.DepositId);
            entity.Property(e => e.DepositId).HasColumnName("deposit_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.PlayerId).HasColumnName("player_id").IsRequired();
            entity.Property(e => e.ProviderId).HasColumnName("provider_id").IsRequired();
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id").IsRequired();
            entity.Property(e => e.TxHash).HasColumnName("tx_hash").IsRequired();
            entity.Property(e => e.AmountMinor).HasColumnName("amount_minor").IsRequired();
            entity.Property(e => e.ConfirmationsReceived).HasColumnName("confirmations_received").HasDefaultValue(0);
            entity.Property(e => e.ConfirmationsRequired).HasColumnName("confirmations_required").HasDefaultValue(1);
            entity.Property(e => e.Status).HasColumnName("status")
                .HasConversion<string>();
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb")
                .HasConversion(
                    v => v.ToJsonString(),
                    v => JsonDocument.Parse(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasIndex(e => e.TxHash).IsUnique();
            entity.HasOne<PaymentProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DepositSession>().WithMany().HasForeignKey(e => e.SessionId).OnDelete(DeleteBehavior.SetNull);
        });

        // Withdrawal Requests
        modelBuilder.Entity<WithdrawalRequest>(entity =>
        {
            entity.ToTable("withdrawal_requests");
            entity.HasKey(e => e.WithdrawalId);
            entity.Property(e => e.WithdrawalId).HasColumnName("withdrawal_id");
            entity.Property(e => e.PlayerId).HasColumnName("player_id").IsRequired();
            entity.Property(e => e.ProviderId).HasColumnName("provider_id").IsRequired();
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id").IsRequired();
            entity.Property(e => e.AmountMinor).HasColumnName("amount_minor").IsRequired();
            entity.Property(e => e.FeeMinor).HasColumnName("fee_minor").HasDefaultValue(0);
            entity.Property(e => e.TargetAddress).HasColumnName("target_address").IsRequired();
            entity.Property(e => e.ProviderReference).HasColumnName("provider_reference");
            entity.Property(e => e.TxHash).HasColumnName("tx_hash");
            entity.Property(e => e.Status).HasColumnName("status")
                .HasConversion<string>();
            entity.Property(e => e.FailReason).HasColumnName("fail_reason");
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb")
                .HasConversion(
                    v => v.ToJsonString(),
                    v => JsonDocument.Parse(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasOne<PaymentProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Restrict);
        });

        // Idempotency Keys
        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("idempotency_keys");
            entity.HasKey(e => new { e.Source, e.IdempotencyKeyValue });
            entity.Property(e => e.Source).HasColumnName("source").IsRequired().HasMaxLength(100);
            entity.Property(e => e.IdempotencyKeyValue).HasColumnName("idempotency_key").IsRequired().HasMaxLength(255);
            entity.Property(e => e.RelatedId).HasColumnName("related_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        });

        // Outbox Events
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("outbox_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityColumn();
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(100);
            entity.Property(e => e.RoutingKey).HasColumnName("routing_key").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb")
                .HasConversion(
                    v => v.ToJsonString(),
                    v => JsonDocument.Parse(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.HasIndex(e => new { e.PublishedAt, e.CreatedAt });
        });

        // Inbox Events
        modelBuilder.Entity<InboxEvent>(entity =>
        {
            entity.ToTable("inbox_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityColumn();
            entity.Property(e => e.Source).HasColumnName("source").IsRequired().HasMaxLength(100);
            entity.Property(e => e.IdempotencyKeyValue).HasColumnName("idempotency_key").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb")
                .HasConversion(
                    v => v.ToJsonString(),
                    v => JsonDocument.Parse(v));
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.HasIndex(e => new { e.Source, e.IdempotencyKeyValue }).IsUnique();
        });
    }
}

