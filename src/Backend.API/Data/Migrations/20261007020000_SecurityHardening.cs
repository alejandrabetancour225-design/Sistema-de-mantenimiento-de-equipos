using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Data.Migrations
{
    /// <summary>
    /// Endurecimiento de seguridad:
    ///  - Users.LockoutEnd: bloqueo temporal por intentos fallidos (en lugar de desactivar la cuenta).
    ///  - Users.SecurityStamp: invalida los tokens al cambiar rol, estado, contraseña o cerrar sesión.
    ///  - AuditLogs: registro de auditoría.
    ///  - Índices únicos parciales contra condiciones de carrera (doble asignación, incidente y mantenimiento).
    /// </summary>
    public partial class SecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los índices únicos fallan si ya hay datos duplicados; se avisa con un mensaje claro.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM ""Assignments"" WHERE ""Status"" = 0 GROUP BY ""EquipmentId"" HAVING COUNT(*) > 1) THEN
        RAISE EXCEPTION 'Hay equipos con más de una asignación ACTIVE. Libera las duplicadas antes de aplicar la migración SecurityHardening.';
    END IF;
    IF EXISTS (SELECT 1 FROM ""Incidents"" WHERE ""Status"" IN (0, 1) GROUP BY ""EquipmentId"" HAVING COUNT(*) > 1) THEN
        RAISE EXCEPTION 'Hay equipos con más de un incidente OPEN/IN_PROGRESS. Ciérralos antes de aplicar la migración SecurityHardening.';
    END IF;
    IF EXISTS (SELECT 1 FROM ""Maintenances"" WHERE ""Status"" IN (0, 1) GROUP BY ""EquipmentId"" HAVING COUNT(*) > 1) THEN
        RAISE EXCEPTION 'Hay equipos con más de un mantenimiento OPEN/IN_PROGRESS. Ciérralos o cancélalos antes de aplicar la migración SecurityHardening.';
    END IF;
END
$$;");

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            // Cada usuario existente recibe un sello aleatorio; luego se quita el valor por defecto
            // porque la aplicación siempre lo asigna.
            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""SecurityStamp"" DROP DEFAULT;");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_EquipmentId_Active",
                table: "Assignments",
                column: "EquipmentId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_EquipmentId_Open",
                table: "Incidents",
                column: "EquipmentId",
                unique: true,
                filter: "\"Status\" IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Maintenances_EquipmentId_Open",
                table: "Maintenances",
                column: "EquipmentId",
                unique: true,
                filter: "\"Status\" IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Maintenances_EquipmentId_Open",
                table: "Maintenances");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_EquipmentId_Open",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_EquipmentId_Active",
                table: "Assignments");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Users");
        }
    }
}
