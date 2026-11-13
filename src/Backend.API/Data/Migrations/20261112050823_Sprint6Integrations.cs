using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint6Integrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalAttributes",
                table: "Equipments",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Equipments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalSourceId",
                table: "Equipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExternalSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EncryptedConnection = table.Column<string>(type: "text", nullable: true),
                    TableName = table.Column<string>(type: "character varying(130)", maxLength: 130, nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FileContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    MappingJson = table.Column<string>(type: "jsonb", nullable: false),
                    KeyColumn = table.Column<string>(type: "text", nullable: false),
                    SyncIntervalHours = table.Column<int>(type: "integer", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncSummary = table.Column<string>(type: "jsonb", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_ExternalSourceId_ExternalId",
                table: "Equipments",
                columns: new[] { "ExternalSourceId", "ExternalId" },
                unique: true,
                filter: "\"ExternalSourceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSources_OrganizationId_Name",
                table: "ExternalSources",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalSources");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_ExternalSourceId_ExternalId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ExternalAttributes",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ExternalSourceId",
                table: "Equipments");
        }
    }
}
