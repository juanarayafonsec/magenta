using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Data;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
    {
    }

    public DbSet<Network> Networks { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<CurrencyNetwork> CurrencyNetworks { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<LedgerTransaction> LedgerTransactions { get; set; }
    public DbSet<LedgerPosting> LedgerPostings { get; set; }
    public DbSet<AccountBalance> AccountBalances { get; set; }
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<InboxEvent> InboxEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Networks
        modelBuilder.Entity<Network>(entity =>
        {
            entity.ToTable("networks");
            entity.HasKey(e => e.NetworkId);
            entity.Property(e => e.NetworkId).HasColumnName("network_id").UseIdentityColumn();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.NativeSymbol).HasColumnName("native_symbol").IsRequired().HasMaxLength(10);
            entity.Property(e => e.ConfirmationsRequired).HasColumnName("confirmations_required").HasDefaultValue(1);
            entity.Property(e => e.ExplorerUrl).HasColumnName("explorer_url");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Currencies
        modelBuilder.Entity<Currency>(entity =>
        {
            entity.ToTable("currencies");
            entity.HasKey(e => e.CurrencyId);
            entity.Property(e => e.CurrencyId).HasColumnName("currency_id").UseIdentityColumn();
            entity.Property(e => e.Code).HasColumnName("code").IsRequired().HasMaxLength(10);
            entity.Property(e => e.DisplayName).HasColumnName("display_name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Decimals).HasColumnName("decimals").IsRequired();
            entity.HasCheckConstraint("CK_Currencies_Decimals", "decimals >= 0 AND decimals <= 18");
            entity.Property(e => e.IconUrl).HasColumnName("icon_url");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Currency Networks
        modelBuilder.Entity<CurrencyNetwork>(entity =>
        {
            entity.ToTable("currency_networks");
            entity.HasKey(e => e.CurrencyNetworkId);
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id").UseIdentityColumn();
            entity.Property(e => e.CurrencyId).HasColumnName("currency_id").IsRequired();
            entity.Property(e => e.NetworkId).HasColumnName("network_id").IsRequired();
            entity.Property(e => e.TokenContract).HasColumnName("token_contract");
            entity.Property(e => e.WithdrawalFeeMinor).HasColumnName("withdrawal_fee_minor").HasDefaultValue(0);
            entity.Property(e => e.MinDepositMinor).HasColumnName("min_deposit_minor").HasDefaultValue(0);
            entity.Property(e => e.MinWithdrawMinor).HasColumnName("min_withdraw_minor").HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasOne<Currency>().WithMany().HasForeignKey(e => e.CurrencyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Network>().WithMany().HasForeignKey(e => e.NetworkId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.CurrencyId, e.NetworkId }).IsUnique();
        });

        // Accounts
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.AccountId).HasColumnName("account_id").UseIdentityColumn();
            entity.Property(e => e.PlayerId).HasColumnName("player_id").IsRequired();
            entity.Property(e => e.CurrencyNetworkId).HasColumnName("currency_network_id").IsRequired();
            entity.Property(e => e.AccountType).HasColumnName("account_type").IsRequired()
                .HasConversion<string>(v => v.ToString().Replace("_", ":"), v => Enum.Parse<Domain.Enums.AccountType>(v.Replace(":", "_")));
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("ACTIVE");
            entity.HasOne<CurrencyNetwork>().WithMany().HasForeignKey(e => e.CurrencyNetworkId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.PlayerId, e.CurrencyNetworkId, e.AccountType }).IsUnique();
        });

        // Ledger Transactions
        modelBuilder.Entity<LedgerTransaction>(entity =>
        {
            entity.ToTable("ledger_transactions");
            entity.HasKey(e => e.TxId);
            entity.Property(e => e.TxId).HasColumnName("tx_id");
            entity.Property(e => e.TxType).HasColumnName("tx_type").IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.ExternalRef).HasColumnName("external_ref");
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb")
                .HasConversion(
                    v => v.ToJsonString(),
                    v => JsonDocument.Parse(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        });

        // Ledger Postings
        modelBuilder.Entity<LedgerPosting>(entity =>
        {
            entity.ToTable("ledger_postings");
            entity.HasKey(e => e.PostingId);
            entity.Property(e => e.PostingId).HasColumnName("posting_id").UseIdentityColumn();
            entity.Property(e => e.TxId).HasColumnName("tx_id").IsRequired();
            entity.Property(e => e.AccountId).HasColumnName("account_id").IsRequired();
            entity.Property(e => e.Direction).HasColumnName("direction").IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.AmountMinor).HasColumnName("amount_minor").IsRequired();
            entity.HasCheckConstraint("CK_LedgerPostings_AmountMinor", "amount_minor >= 0");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasOne<LedgerTransaction>().WithMany().HasForeignKey(e => e.TxId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TxId);
            entity.HasIndex(e => e.AccountId);
        });

        // Account Balances
        modelBuilder.Entity<AccountBalance>(entity =>
        {
            entity.ToTable("account_balances");
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.BalanceMinor).HasColumnName("balance_minor").HasDefaultValue(0);
            entity.Property(e => e.ReservedMinor).HasColumnName("reserved_minor").HasDefaultValue(0);
            entity.Property(e => e.CashableMinor).HasColumnName("cashable_minor").HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        // Idempotency Keys
        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("idempotency_keys");
            entity.HasKey(e => new { e.Source, e.IdempotencyKeyValue });
            entity.Property(e => e.Source).HasColumnName("source").IsRequired().HasMaxLength(100);
            entity.Property(e => e.IdempotencyKeyValue).HasColumnName("idempotency_key").IsRequired().HasMaxLength(255);
            entity.Property(e => e.TxId).HasColumnName("tx_id").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasOne<LedgerTransaction>().WithMany().HasForeignKey(e => e.TxId).OnDelete(DeleteBehavior.Restrict);
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

