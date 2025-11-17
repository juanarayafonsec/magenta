using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Magenta.Wallet.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class CreateWalletSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Networks
        migrationBuilder.CreateTable(
            name: "networks",
            columns: table => new
            {
                network_id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "text", nullable: false),
                native_symbol = table.Column<string>(type: "text", nullable: false),
                confirmations_required = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                explorer_url = table.Column<string>(type: "text", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_networks", x => x.network_id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_networks_name",
            table: "networks",
            column: "name",
            unique: true);

        // Currencies
        migrationBuilder.CreateTable(
            name: "currencies",
            columns: table => new
            {
                currency_id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                code = table.Column<string>(type: "text", nullable: false),
                display_name = table.Column<string>(type: "text", nullable: false),
                decimals = table.Column<int>(type: "integer", nullable: false),
                icon_url = table.Column<string>(type: "text", nullable: true),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_currencies", x => x.currency_id);
                table.CheckConstraint("CK_Currencies_Decimals", "decimals >= 0 AND decimals <= 18");
            });

        migrationBuilder.CreateIndex(
            name: "IX_currencies_code",
            table: "currencies",
            column: "code",
            unique: true);

        // Currency Networks
        migrationBuilder.CreateTable(
            name: "currency_networks",
            columns: table => new
            {
                currency_network_id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                currency_id = table.Column<int>(type: "integer", nullable: false),
                network_id = table.Column<int>(type: "integer", nullable: false),
                token_contract = table.Column<string>(type: "text", nullable: true),
                withdrawal_fee_minor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                min_deposit_minor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                min_withdraw_minor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_currency_networks", x => x.currency_network_id);
                table.ForeignKey(
                    name: "FK_currency_networks_currencies_currency_id",
                    column: x => x.currency_id,
                    principalTable: "currencies",
                    principalColumn: "currency_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_currency_networks_networks_network_id",
                    column: x => x.network_id,
                    principalTable: "networks",
                    principalColumn: "network_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_currency_networks_currency_id_network_id",
            table: "currency_networks",
            columns: new[] { "currency_id", "network_id" },
            unique: true);

        // Accounts
        migrationBuilder.CreateTable(
            name: "accounts",
            columns: table => new
            {
                account_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                player_id = table.Column<long>(type: "bigint", nullable: false),
                currency_network_id = table.Column<int>(type: "integer", nullable: false),
                account_type = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "ACTIVE")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_accounts", x => x.account_id);
                table.ForeignKey(
                    name: "FK_accounts_currency_networks_currency_network_id",
                    column: x => x.currency_network_id,
                    principalTable: "currency_networks",
                    principalColumn: "currency_network_id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_Accounts_AccountType", "account_type IN ('MAIN','WITHDRAW_HOLD','BONUS','HOUSE','HOUSE:WAGER','HOUSE:FEES')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_accounts_player_id_currency_network_id_account_type",
            table: "accounts",
            columns: new[] { "player_id", "currency_network_id", "account_type" },
            unique: true);

        // Ledger Transactions
        migrationBuilder.CreateTable(
            name: "ledger_transactions",
            columns: table => new
            {
                tx_id = table.Column<Guid>(type: "uuid", nullable: false),
                tx_type = table.Column<string>(type: "text", nullable: false),
                external_ref = table.Column<string>(type: "text", nullable: true),
                metadata = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ledger_transactions", x => x.tx_id);
            });

        // Ledger Postings
        migrationBuilder.CreateTable(
            name: "ledger_postings",
            columns: table => new
            {
                posting_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                tx_id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<long>(type: "bigint", nullable: false),
                direction = table.Column<string>(type: "text", nullable: false),
                amount_minor = table.Column<long>(type: "bigint", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ledger_postings", x => x.posting_id);
                table.ForeignKey(
                    name: "FK_ledger_postings_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "accounts",
                    principalColumn: "account_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ledger_postings_ledger_transactions_tx_id",
                    column: x => x.tx_id,
                    principalTable: "ledger_transactions",
                    principalColumn: "tx_id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_LedgerPostings_AmountMinor", "amount_minor >= 0");
                table.CheckConstraint("CK_LedgerPostings_Direction", "direction IN ('DEBIT','CREDIT')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_ledger_postings_tx_id",
            table: "ledger_postings",
            column: "tx_id");

        migrationBuilder.CreateIndex(
            name: "IX_ledger_postings_account_id",
            table: "ledger_postings",
            column: "account_id");

        // Account Balances
        migrationBuilder.CreateTable(
            name: "account_balances",
            columns: table => new
            {
                account_id = table.Column<long>(type: "bigint", nullable: false),
                balance_minor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                reserved_minor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                cashable_minor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_account_balances", x => x.account_id);
                table.ForeignKey(
                    name: "FK_account_balances_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "accounts",
                    principalColumn: "account_id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Idempotency Keys
        migrationBuilder.CreateTable(
            name: "idempotency_keys",
            columns: table => new
            {
                source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                idempotency_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                tx_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_idempotency_keys", x => new { x.source, x.idempotency_key });
                table.ForeignKey(
                    name: "FK_idempotency_keys_ledger_transactions_tx_id",
                    column: x => x.tx_id,
                    principalTable: "ledger_transactions",
                    principalColumn: "tx_id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Outbox Events
        migrationBuilder.CreateTable(
            name: "outbox_events",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                routing_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_events", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_events_published_at_created_at",
            table: "outbox_events",
            columns: new[] { "published_at", "created_at" });

        // Inbox Events
        migrationBuilder.CreateTable(
            name: "inbox_events",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                idempotency_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inbox_events", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_inbox_events_source_idempotency_key",
            table: "inbox_events",
            columns: new[] { "source", "idempotency_key" },
            unique: true);

        // View: v_player_currency_balances
        migrationBuilder.Sql(@"
            CREATE OR REPLACE VIEW v_player_currency_balances AS
            SELECT
                c.code AS currency_code,
                n.name AS network,
                a.player_id,
                COALESCE(ab.balance_minor, 0) AS balance_minor,
                COALESCE(ab.cashable_minor, 0) AS cashable_minor,
                COALESCE(ab.reserved_minor, 0) AS reserved_minor
            FROM currency_networks cn
            JOIN currencies c ON c.currency_id = cn.currency_id
            JOIN networks n ON n.network_id = cn.network_id
            LEFT JOIN accounts a ON a.currency_network_id = cn.currency_network_id AND a.account_type = 'MAIN'
            LEFT JOIN account_balances ab ON ab.account_id = a.account_id
            WHERE c.is_active AND n.is_active;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_player_currency_balances;");
        
        migrationBuilder.DropTable(name: "inbox_events");
        migrationBuilder.DropTable(name: "outbox_events");
        migrationBuilder.DropTable(name: "idempotency_keys");
        migrationBuilder.DropTable(name: "account_balances");
        migrationBuilder.DropTable(name: "ledger_postings");
        migrationBuilder.DropTable(name: "ledger_transactions");
        migrationBuilder.DropTable(name: "accounts");
        migrationBuilder.DropTable(name: "currency_networks");
        migrationBuilder.DropTable(name: "currencies");
        migrationBuilder.DropTable(name: "networks");
    }
}

