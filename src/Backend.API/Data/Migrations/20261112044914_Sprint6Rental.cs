using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint6Rental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Ownership",
                table: "Equipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RentalContractId",
                table: "Equipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacesEquipmentId",
                table: "Equipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAt",
                table: "Equipments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Lessors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TaxId = table.Column<string>(type: "text", nullable: true),
                    ContactName = table.Column<string>(type: "text", nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RentalContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AllowsInternalPreventive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ExpiryNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalContracts_Lessors_LessorId",
                        column: x => x.LessorId,
                        principalTable: "Lessors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessorClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LessorResponse = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessorClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessorClaims_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessorClaims_RentalContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "RentalContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RentalRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MonthlyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalRates_RentalContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "RentalContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_RentalContractId",
                table: "Equipments",
                column: "RentalContractId");

            migrationBuilder.CreateIndex(
                name: "IX_LessorClaims_ContractId",
                table: "LessorClaims",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_LessorClaims_EquipmentId",
                table: "LessorClaims",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LessorClaims_Status",
                table: "LessorClaims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Lessors_OrganizationId_Name",
                table: "Lessors",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalContracts_EndDate",
                table: "RentalContracts",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_RentalContracts_LessorId",
                table: "RentalContracts",
                column: "LessorId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalContracts_OrganizationId_ContractNumber",
                table: "RentalContracts",
                columns: new[] { "OrganizationId", "ContractNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalRates_ContractId_EquipmentType",
                table: "RentalRates",
                columns: new[] { "ContractId", "EquipmentType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_RentalContracts_RentalContractId",
                table: "Equipments",
                column: "RentalContractId",
                principalTable: "RentalContracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_RentalContracts_RentalContractId",
                table: "Equipments");

            migrationBuilder.DropTable(
                name: "LessorClaims");

            migrationBuilder.DropTable(
                name: "RentalRates");

            migrationBuilder.DropTable(
                name: "RentalContracts");

            migrationBuilder.DropTable(
                name: "Lessors");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_RentalContractId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Ownership",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "RentalContractId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ReplacesEquipmentId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "Equipments");
        }
    }
}
