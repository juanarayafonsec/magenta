using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Data;

public class WalletDbContext : DbContext
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
    public WalletDbContext(DbContextOptions<WalletDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<LedgerTransaction> LedgerTransactions { get; set; }
    public DbSet<LedgerPosting> LedgerPostings { get; set; }
    public DbSet<CurrencyNetwork> CurrencyNetworks { get; set; }
    public DbSet<CurrencyCatalog> CurrencyCatalogs { get; set; }
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<InboxEvent> InboxEvents { get; set; }
    public DbSet<AccountBalanceDerived> AccountBalancesDerived { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Account
        builder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.AccountId).HasColumnName("account_id").UseIdentityColumn();
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.AccountType).HasColumnName("account_type").IsRequired();
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id").IsRequired();

            // Unique constraint: UNIQUE(player_id, currency_network_id, account_type)
            entity.HasIndex(e => new { e.PlayerId, e.CurrencyNetworkId, e.AccountType })
                .IsUnique()
                .HasDatabaseName("IX_accounts_player_currency_account");

            entity.HasOne(e => e.CurrencyNetwork)
                .WithMany()
                .HasForeignKey(e => e.CurrencyNetworkId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure CurrencyNetwork
        builder.Entity<CurrencyNetwork>(entity =>
        {
            entity.ToTable("currency_networks");
            entity.HasKey(e => e.CurrencyNetworkId);
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id").UseIdentityColumn();
            entity.Property(e => e.Currency).HasColumnName("currency").IsRequired();
            entity.Property(e => e.Network).HasColumnName("network").IsRequired();
            entity.Property(e => e.Decimals).HasColumnName("decimals").IsRequired();

            entity.HasIndex(e => new { e.Currency, e.Network })
                .IsUnique()
                .HasDatabaseName("IX_currency_networks_currency_network");
        });

        // Configure CurrencyCatalog
        builder.Entity<CurrencyCatalog>(entity =>
        {
            entity.ToTable("currency_catalog");
            entity.HasKey(e => e.CurrencyCatalogId);
            entity.Property(e => e.CurrencyCatalogId).HasColumnName("currency_catalog_id").UseIdentityColumn();
            entity.Property(e => e.Currency).HasColumnName("currency").IsRequired();
            entity.Property(e => e.Decimals).HasColumnName("decimals").IsRequired();
            entity.Property(e => e.Symbol).HasColumnName("symbol").IsRequired();
        });

        // Configure LedgerTransaction
        builder.Entity<LedgerTransaction>(entity =>
        {
            entity.ToTable("ledger_transactions");
            entity.HasKey(e => e.LedgerTransactionId);
            entity.Property(e => e.LedgerTransactionId).HasColumnName("ledger_transaction_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ReferenceType).HasColumnName("reference_type").IsRequired();
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id").IsRequired();
            entity.Property(e => e.Source).HasColumnName("source").IsRequired();
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => SerializeJsonDocument(v),
                    v => DeserializeJsonDocument(v));

            entity.HasMany(e => e.Postings)
                .WithOne(p => p.LedgerTransaction)
                .HasForeignKey(p => p.LedgerTransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure LedgerPosting
        builder.Entity<LedgerPosting>(entity =>
        {
            entity.ToTable("ledger_postings");
            entity.HasKey(e => e.LedgerPostingId);
            entity.Property(e => e.LedgerPostingId).HasColumnName("ledger_posting_id").UseIdentityColumn();
            entity.Property(e => e.LedgerTransactionId).HasColumnName("ledger_transaction_id").IsRequired();
            entity.Property(e => e.AccountId).HasColumnName("account_id").IsRequired();
            entity.Property(e => e.Direction).HasColumnName("direction").IsRequired().HasMaxLength(2);
            entity.Property(e => e.AmountMinor).HasColumnName("amount_minor").HasColumnType("BIGINT").IsRequired();

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure IdempotencyKey
        builder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("idempotency_keys");
            entity.HasKey(e => new { e.Source, e.IdempotencyKeyValue });
            entity.Property(e => e.Source).HasColumnName("source").IsRequired();
            entity.Property(e => e.IdempotencyKeyValue).HasColumnName("idempotency_key").IsRequired();
            entity.Property(e => e.TransactionId).HasColumnName("tx_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure OutboxEvent
        builder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("outbox_events");
            entity.HasKey(e => e.OutboxEventId);
            entity.Property(e => e.OutboxEventId).HasColumnName("outbox_event_id").UseIdentityColumn();
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired();
            entity.Property(e => e.RoutingKey).HasColumnName("routing_key").IsRequired();
            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired()
                .HasConversion(
                    v => SerializeJsonDocumentRequired(v),
                    v => DeserializeJsonDocumentRequired(v));
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");

            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("IX_outbox_events_status_created");
        });

        // Configure InboxEvent
        builder.Entity<InboxEvent>(entity =>
        {
            entity.ToTable("inbox_events");
            entity.HasKey(e => e.InboxEventId);
            entity.Property(e => e.InboxEventId).HasColumnName("inbox_event_id").UseIdentityColumn();
            entity.Property(e => e.Source).HasColumnName("source").IsRequired();
            entity.Property(e => e.MessageId).HasColumnName("message_id").IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired();
            entity.Property(e => e.RoutingKey).HasColumnName("routing_key").IsRequired();
            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired()
                .HasConversion(
                    v => SerializeJsonDocumentRequired(v),
                    v => DeserializeJsonDocumentRequired(v));
            entity.Property(e => e.ReceivedAt).HasColumnName("received_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");

            entity.HasIndex(e => new { e.Source, e.MessageId })
                .IsUnique()
                .HasDatabaseName("IX_inbox_events_source_message_id");
            entity.HasIndex(e => new { e.ProcessedAt, e.ReceivedAt })
                .HasDatabaseName("IX_inbox_events_processed_received");
        });

        // Configure AccountBalanceDerived
        builder.Entity<AccountBalanceDerived>(entity =>
        {
            entity.ToTable("account_balances_derived");
            entity.HasKey(e => e.AccountBalanceDerivedId);
            entity.Property(e => e.AccountBalanceDerivedId).HasColumnName("account_balance_derived_id").UseIdentityColumn();
            entity.Property(e => e.AccountId).HasColumnName("account_id").IsRequired();
            entity.Property(e => e.BalanceMinor).HasColumnName("balance_minor").HasColumnType("BIGINT").IsRequired();
            entity.Property(e => e.LastUpdatedAt).HasColumnName("last_updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.AccountId)
                .IsUnique()
                .HasDatabaseName("IX_account_balances_derived_account_id");
        });
    }
}
