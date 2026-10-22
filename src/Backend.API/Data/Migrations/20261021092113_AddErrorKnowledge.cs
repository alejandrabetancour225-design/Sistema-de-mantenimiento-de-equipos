using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorKnowledge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorKnowledges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Symptom = table.Column<string>(type: "text", nullable: false),
                    ComponentType = table.Column<string>(type: "text", nullable: false),
                    PossibleCauses = table.Column<string>(type: "text", nullable: false),
                    SuggestedSolution = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorKnowledges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorKnowledges_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorKnowledges_Active",
                table: "ErrorKnowledges",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorKnowledges_ComponentType",
                table: "ErrorKnowledges",
                column: "ComponentType");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorKnowledges_CreatedByUserId",
                table: "ErrorKnowledges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorKnowledges_Symptom",
                table: "ErrorKnowledges",
                column: "Symptom");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorKnowledges");
        }
    }
}
