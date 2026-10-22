using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAIFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosisId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: false),
                    PredictedCause = table.Column<string>(type: "text", nullable: false),
                    ActualCause = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIFeedbacks_AIDiagnoses_DiagnosisId",
                        column: x => x.DiagnosisId,
                        principalTable: "AIDiagnoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AIFeedbacks_Users_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIFeedbacks_CreatedAt",
                table: "AIFeedbacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIFeedbacks_DiagnosisId",
                table: "AIFeedbacks",
                column: "DiagnosisId");

            migrationBuilder.CreateIndex(
                name: "IX_AIFeedbacks_IsCorrect",
                table: "AIFeedbacks",
                column: "IsCorrect");

            migrationBuilder.CreateIndex(
                name: "IX_AIFeedbacks_TechnicianId",
                table: "AIFeedbacks",
                column: "TechnicianId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIFeedbacks");
        }
    }
}
