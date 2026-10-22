using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFailurePrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FailurePredictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    RiskComponent = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ModelVersion = table.Column<string>(type: "text", nullable: false),
                    Acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    PredictedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailurePredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FailurePredictions_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailurePredictions_Acknowledged",
                table: "FailurePredictions",
                column: "Acknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_FailurePredictions_EquipmentId",
                table: "FailurePredictions",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FailurePredictions_PredictedAt",
                table: "FailurePredictions",
                column: "PredictedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FailurePredictions_RiskScore",
                table: "FailurePredictions",
                column: "RiskScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailurePredictions");
        }
    }
}
