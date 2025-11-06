using Magenta.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

        // Apply all configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
    }
}




