using Magenta.Payment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Magenta.Payment.Infrastructure.Data;

public class PaymentDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions();

    private static string SerializeJsonDocument(JsonDocument? doc)
    {
        if (doc == null) return null!;
        return JsonSerializer.Serialize(doc, JsonOptions);
    }

    private static JsonDocument? DeserializeJsonDocument(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        return JsonDocument.Parse(json);
    }

    private static string SerializeJsonDocumentRequired(JsonDocument doc)
    {
        return JsonSerializer.Serialize(doc, JsonOptions);
    }

    private static JsonDocument DeserializeJsonDocumentRequired(string json)
    {
        return JsonDocument.Parse(json);
    }

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentProvider> PaymentProviders { get; set; }
    public DbSet<DepositSession> DepositSessions { get; set; }
    public DbSet<DepositRequest> DepositRequests { get; set; }
    public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<InboxEvent> InboxEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure PaymentProvider
        builder.Entity<PaymentProvider>(entity =>
        {
            entity.ToTable("payment_providers");
            entity.HasKey(e => e.ProviderId);
            entity.Property(e => e.ProviderId).HasColumnName("provider_id").UseIdentityColumn();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").IsRequired();
            entity.Property(e => e.ApiBaseUrl).HasColumnName("api_base_url").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure DepositSession
        builder.Entity<DepositSession>(entity =>
        {
            entity.ToTable("deposit_sessions");
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.ProviderId).HasColumnName("provider_id");
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id");
            entity.Property(e => e.Address).HasColumnName("address").IsRequired();
            entity.Property(e => e.MemoOrTag).HasColumnName("memo_or_tag");
            entity.Property(e => e.ProviderReference).HasColumnName("provider_reference");
            entity.Property(e => e.ExpectedAmountMinor).HasColumnName("expected_amount_minor");
            entity.Property(e => e.ConfirmationsRequired).HasColumnName("confirmations_required");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => SerializeJsonDocument(v),
                    v => DeserializeJsonDocument(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.PlayerId).HasDatabaseName("IX_deposit_sessions_player_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_deposit_sessions_status");
        });

        // Configure DepositRequest
        builder.Entity<DepositRequest>(entity =>
        {
            entity.ToTable("deposit_requests");
            entity.HasKey(e => e.DepositId);
            entity.Property(e => e.DepositId).HasColumnName("deposit_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.ProviderId).HasColumnName("provider_id");
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id");
            entity.Property(e => e.TxHash).HasColumnName("tx_hash").IsRequired();
            entity.Property(e => e.AmountMinor).HasColumnName("amount_minor").HasColumnType("BIGINT").IsRequired();
            entity.Property(e => e.ConfirmationsReceived).HasColumnName("confirmations_received");
            entity.Property(e => e.ConfirmationsRequired).HasColumnName("confirmations_required");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => SerializeJsonDocument(v),
                    v => DeserializeJsonDocument(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.TxHash).IsUnique().HasDatabaseName("IX_deposit_requests_tx_hash");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_deposit_requests_status");
            entity.HasIndex(e => e.PlayerId).HasDatabaseName("IX_deposit_requests_player_id");
        });

        // Configure WithdrawalRequest
        builder.Entity<WithdrawalRequest>(entity =>
        {
            entity.ToTable("withdrawal_requests");
            entity.HasKey(e => e.WithdrawalId);
            entity.Property(e => e.WithdrawalId).HasColumnName("withdrawal_id");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.ProviderId).HasColumnName("provider_id");
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id");
            entity.Property(e => e.AmountMinor).HasColumnName("amount_minor").HasColumnType("BIGINT").IsRequired();
            entity.Property(e => e.FeeMinor).HasColumnName("fee_minor").HasColumnType("BIGINT");
            entity.Property(e => e.TargetAddress).HasColumnName("target_address").IsRequired();
            entity.Property(e => e.ProviderReference).HasColumnName("provider_reference");
            entity.Property(e => e.TxHash).HasColumnName("tx_hash");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
            entity.Property(e => e.FailReason).HasColumnName("fail_reason");
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => SerializeJsonDocument(v),
                    v => DeserializeJsonDocument(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Status).HasDatabaseName("IX_withdrawal_requests_status");
            entity.HasIndex(e => e.PlayerId).HasDatabaseName("IX_withdrawal_requests_player_id");
        });

        // Configure IdempotencyKey
        builder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("idempotency_keys");
            entity.HasKey(e => new { e.Source, e.IdempotencyKeyValue });
            entity.Property(e => e.Source).HasColumnName("source").IsRequired();
            entity.Property(e => e.IdempotencyKeyValue).HasColumnName("idempotency_key").IsRequired();
            entity.Property(e => e.TxId).HasColumnName("tx_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure OutboxEvent
        builder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("outbox_events");
            entity.HasKey(e => e.OutboxEventId);
            entity.Property(e => e.OutboxEventId).HasColumnName("id").UseIdentityColumn();
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired();
            entity.Property(e => e.RoutingKey).HasColumnName("routing_key").IsRequired();
            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired()
                .HasConversion(
                    v => SerializeJsonDocumentRequired(v),
                    v => DeserializeJsonDocumentRequired(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.PublishAttempts).HasColumnName("publish_attempts").HasDefaultValue(0);
            entity.Property(e => e.LastError).HasColumnName("last_error");

            entity.HasIndex(e => new { e.PublishedAt, e.CreatedAt })
                .HasDatabaseName("IX_outbox_events_published_created");
        });

        // Configure InboxEvent
        builder.Entity<InboxEvent>(entity =>
        {
            entity.ToTable("inbox_events");
            entity.HasKey(e => e.InboxEventId);
            entity.Property(e => e.InboxEventId).HasColumnName("id").UseIdentityColumn();
            entity.Property(e => e.Source).HasColumnName("source").IsRequired();
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired()
                .HasConversion(
                    v => SerializeJsonDocumentRequired(v),
                    v => DeserializeJsonDocumentRequired(v));
            entity.Property(e => e.ReceivedAt).HasColumnName("received_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.LastError).HasColumnName("last_error");

            entity.HasIndex(e => new { e.Source, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("IX_inbox_events_source_idempotency");
            entity.HasIndex(e => new { e.ProcessedAt, e.ReceivedAt })
                .HasDatabaseName("IX_inbox_events_processed_received");
        });
    }
}
