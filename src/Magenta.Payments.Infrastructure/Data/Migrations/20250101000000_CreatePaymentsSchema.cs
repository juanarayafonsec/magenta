using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Magenta.Payments.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class CreatePaymentsSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Payment Providers
        migrationBuilder.CreateTable(
            name: "payment_providers",
            columns: table => new
            {
                provider_id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "text", nullable: false),
                type = table.Column<string>(type: "text", nullable: false),
                api_base_url = table.Column<string>(type: "text", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_payment_providers", x => x.provider_id);
                table.CheckConstraint("CK_PaymentProviders_Type", "type IN ('CRYPTO','FIAT')");
            });

        // Deposit Sessions
        migrationBuilder.CreateTable(
            name: "deposit_sessions",
            columns: table => new
            {
                session_id = table.Column<Guid>(type: "uuid", nullable: false),
                player_id = table.Column<long>(type: "bigint", nullable: false),
                provider_id = table.Column<int>(type: "integer", nullable: false),
                currency_network_id = table.Column<int>(type: "integer", nullable: false),
                address = table.Column<string>(type: "text", nullable: false),
                memo_or_tag = table.Column<string>(type: "text", nullable: true),
                provider_reference = table.Column<string>(type: "text", nullable: true),
                expected_amount_minor = table.Column<long>(type: "bigint", nullable: true),
                min_amount_minor = table.Column<long>(type: "bigint", nullable: true),
                confirmations_required = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                status = table.Column<string>(type: "text", nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                metadata = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_deposit_sessions", x => x.session_id);
                table.ForeignKey(
                    name: "FK_deposit_sessions_payment_providers_provider_id",
                    column: x => x.provider_id,
                    principalTable: "payment_providers",
                    principalColumn: "provider_id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_DepositSessions_Status", "status IN ('OPEN','EXPIRED','COMPLETED')");
            });

        // Deposit Requests
        migrationBuilder.CreateTable(
            name: "deposit_requests",
            columns: table => new
            {
                deposit_id = table.Column<Guid>(type: "uuid", nullable: false),
                session_id = table.Column<Guid>(type: "uuid", nullable: true),
                player_id = table.Column<long>(type: "bigint", nullable: false),
                provider_id = table.Column<int>(type: "integer", nullable: false),
                currency_network_id = table.Column<int>(type: "integer", nullable: false),
                tx_hash = table.Column<string>(type: "text", nullable: false),
                amount_minor = table.Column<long>(type: "bigint", nullable: false),
                confirmations_received = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                confirmations_required = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                status = table.Column<string>(type: "text", nullable: true),
                metadata = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_deposit_requests", x => x.deposit_id);
                table.ForeignKey(
                    name: "FK_deposit_requests_payment_providers_provider_id",
                    column: x => x.provider_id,
                    principalTable: "payment_providers",
                    principalColumn: "provider_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_deposit_requests_deposit_sessions_session_id",
                    column: x => x.session_id,
                    principalTable: "deposit_sessions",
                    principalColumn: "session_id",
                    onDelete: ReferentialAction.SetNull);
                table.CheckConstraint("CK_DepositRequests_Status", "status IN ('PENDING','CONFIRMED','SETTLED','FAILED')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_deposit_requests_tx_hash",
            table: "deposit_requests",
            column: "tx_hash",
            unique: true);

        // Withdrawal Requests
        migrationBuilder.CreateTable(
            name: "withdrawal_requests",
            columns: table => new
            {
                withdrawal_id = table.Column<Guid>(type: "uuid", nullable: false),
                player_id = table.Column<long>(type: "bigint", nullable: false),
                provider_id = table.Column<int>(type: "integer", nullable: false),
                currency_network_id = table.Column<int>(type: "integer", nullable: false),
                amount_minor = table.Column<long>(type: "bigint", nullable: false),
                fee_minor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                target_address = table.Column<string>(type: "text", nullable: false),
                provider_reference = table.Column<string>(type: "text", nullable: true),
                tx_hash = table.Column<string>(type: "text", nullable: true),
                status = table.Column<string>(type: "text", nullable: true),
                fail_reason = table.Column<string>(type: "text", nullable: true),
                metadata = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_withdrawal_requests", x => x.withdrawal_id);
                table.ForeignKey(
                    name: "FK_withdrawal_requests_payment_providers_provider_id",
                    column: x => x.provider_id,
                    principalTable: "payment_providers",
                    principalColumn: "provider_id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_WithdrawalRequests_Status", "status IN ('REQUESTED','PROCESSING','BROADCASTED','SETTLED','FAILED')");
            });

        // Idempotency Keys
        migrationBuilder.CreateTable(
            name: "idempotency_keys",
            columns: table => new
            {
                source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                idempotency_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                related_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_idempotency_keys", x => new { x.source, x.idempotency_key });
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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "inbox_events");
        migrationBuilder.DropTable(name: "outbox_events");
        migrationBuilder.DropTable(name: "idempotency_keys");
        migrationBuilder.DropTable(name: "withdrawal_requests");
        migrationBuilder.DropTable(name: "deposit_requests");
        migrationBuilder.DropTable(name: "deposit_sessions");
        migrationBuilder.DropTable(name: "payment_providers");
    }
}

