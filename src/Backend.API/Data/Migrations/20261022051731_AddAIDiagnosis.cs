using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAIDiagnosis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIDiagnoses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symptom = table.Column<string>(type: "text", nullable: false),
                    RankedCausesJson = table.Column<string>(type: "jsonb", nullable: false),
                    RequiresTechnician = table.Column<bool>(type: "boolean", nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: false),
                    ModelVersion = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIDiagnoses_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIDiagnoses_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AIDiagnoses_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIDiagnoses_CreatedAt",
                table: "AIDiagnoses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIDiagnoses_CreatedByUserId",
                table: "AIDiagnoses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AIDiagnoses_EquipmentId",
                table: "AIDiagnoses",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AIDiagnoses_IncidentId",
                table: "AIDiagnoses",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_AIDiagnoses_Status",
                table: "AIDiagnoses",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIDiagnoses");
        }
    }
}
