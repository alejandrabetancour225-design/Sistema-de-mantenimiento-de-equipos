using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint6MultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SpareParts_Name",
                table: "SpareParts");

            migrationBuilder.DropIndex(
                name: "IX_SlangTerms_Expression",
                table: "SlangTerms");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_InternalCode",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_SerialNumber",
                table: "Equipments");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "SpareParts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "SlangTerms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Maintenances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Incidents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "FailurePredictions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ErrorKnowledges",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Equipments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "EquipmentComponents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "AIFeedbacks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "AIDiagnoses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("0e000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Plan = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "Active", "CreatedAt", "Name", "Plan", "UpdatedAt" },
                values: new object[] { new Guid("0e000000-0000-0000-0000-000000000001"), true, new DateTime(2026, 11, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Organización por defecto", 1, new DateTime(2026, 11, 12, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                column: "Name",
                value: "Gerente");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { new Guid("a0000000-0000-0000-0000-000000000005"), "SuperAdmin" });


            // Los datos existentes pasan a la "Organización por defecto" (Q24) y se quita el valor por
            // defecto: de aquí en adelante la API siempre indica la organización de cada registro.
            migrationBuilder.Sql(@"
UPDATE ""Users"" SET ""OrganizationId"" = '0e000000-0000-0000-0000-000000000001' WHERE ""OrganizationId"" IS NULL;
UPDATE ""AuditLogs"" SET ""OrganizationId"" = '0e000000-0000-0000-0000-000000000001' WHERE ""OrganizationId"" IS NULL;
ALTER TABLE ""SpareParts"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
ALTER TABLE ""Maintenances"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
ALTER TABLE ""Incidents"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
ALTER TABLE ""FailurePredictions"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
ALTER TABLE ""ErrorKnowledges"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
ALTER TABLE ""Equipments"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
ALTER TABLE ""EquipmentComponents"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
ALTER TABLE ""Assignments"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
ALTER TABLE ""AIFeedbacks"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
ALTER TABLE ""AIDiagnoses"" ALTER COLUMN ""OrganizationId"" DROP DEFAULT;
");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_OrganizationId_Name",
                table: "SpareParts",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlangTerms_OrganizationId_Expression",
                table: "SlangTerms",
                columns: new[] { "OrganizationId", "Expression" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_OrganizationId_InternalCode",
                table: "Equipments",
                columns: new[] { "OrganizationId", "InternalCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_OrganizationId_SerialNumber",
                table: "Equipments",
                columns: new[] { "OrganizationId", "SerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Organizations_OrganizationId",
                table: "Equipments",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Organizations_OrganizationId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Users_OrganizationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SpareParts_OrganizationId_Name",
                table: "SpareParts");

            migrationBuilder.DropIndex(
                name: "IX_SlangTerms_OrganizationId_Expression",
                table: "SlangTerms");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_OrganizationId_InternalCode",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_OrganizationId_SerialNumber",
                table: "Equipments");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000005"));

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SpareParts");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SlangTerms");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "FailurePredictions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ErrorKnowledges");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "EquipmentComponents");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AIFeedbacks");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AIDiagnoses");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"),
                column: "Name",
                value: "Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_Name",
                table: "SpareParts",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlangTerms_Expression",
                table: "SlangTerms",
                column: "Expression",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_InternalCode",
                table: "Equipments",
                column: "InternalCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_SerialNumber",
                table: "Equipments",
                column: "SerialNumber",
                unique: true);
        }
    }
}
