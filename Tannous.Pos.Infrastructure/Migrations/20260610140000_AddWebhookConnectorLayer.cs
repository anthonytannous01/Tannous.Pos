using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Open API / webhook connector: subscriptions, delivery audit logs, and integrator API keys.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260610140000_AddWebhookConnectorLayer")]
public partial class AddWebhookConnectorLayer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WebhookSubscriptions",
            columns: table => new
            {
                Id               = table.Column<Guid>(type: "uuid", nullable: false),
                Name             = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                EndpointUrl      = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Secret           = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                IsActive         = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                BranchId         = table.Column<Guid>(type: "uuid", nullable: true),
                SubscribedEvents = table.Column<string>(type: "text", nullable: false),
                CreatedAt        = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt        = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy        = table.Column<string>(type: "text", nullable: true),
                UpdatedBy        = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_WebhookSubscriptions_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "ApiKeys",
            columns: table => new
            {
                Id         = table.Column<Guid>(type: "uuid", nullable: false),
                Name       = table.Column<string>(type: "text", nullable: false),
                KeyHash    = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                KeyPrefix  = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                IsActive   = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                BranchId   = table.Column<Guid>(type: "uuid", nullable: true),
                ExpiresAt  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt  = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy  = table.Column<string>(type: "text", nullable: true),
                UpdatedBy  = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiKeys", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApiKeys_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "WebhookDeliveryLogs",
            columns: table => new
            {
                Id             = table.Column<Guid>(type: "uuid", nullable: false),
                SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                EventType      = table.Column<int>(type: "integer", nullable: false),
                EventId        = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                Payload        = table.Column<string>(type: "text", nullable: false),
                ResponseCode   = table.Column<int>(type: "integer", nullable: true),
                IsSuccess      = table.Column<bool>(type: "boolean", nullable: false),
                ErrorMessage   = table.Column<string>(type: "text", nullable: true),
                AttemptNumber  = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                DurationMs     = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt      = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt      = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy      = table.Column<string>(type: "text", nullable: true),
                UpdatedBy      = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WebhookDeliveryLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_WebhookDeliveryLogs_WebhookSubscriptions_SubscriptionId",
                    column: x => x.SubscriptionId,
                    principalTable: "WebhookSubscriptions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WebhookSubscriptions_BranchId_IsActive",
            table: "WebhookSubscriptions",
            columns: new[] { "BranchId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_WebhookDeliveryLogs_SubscriptionId_CreatedAt",
            table: "WebhookDeliveryLogs",
            columns: new[] { "SubscriptionId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ApiKeys_KeyHash",
            table: "ApiKeys",
            column: "KeyHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ApiKeys_BranchId_IsActive",
            table: "ApiKeys",
            columns: new[] { "BranchId", "IsActive" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "WebhookDeliveryLogs");
        migrationBuilder.DropTable(name: "ApiKeys");
        migrationBuilder.DropTable(name: "WebhookSubscriptions");
    }
}
