using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint6StateSyncAndVoid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Maintenances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Maintenances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedById",
                table: "Maintenances",
                type: "uuid",
                nullable: true);

            // Código interno y serie se guardan en mayúsculas (el servicio ya los normaliza al guardar).
            migrationBuilder.Sql(@"
UPDATE ""Equipments""
SET ""InternalCode"" = upper(trim(""InternalCode"")),
    ""SerialNumber"" = upper(trim(""SerialNumber""))
WHERE ""InternalCode"" <> upper(trim(""InternalCode""))
   OR ""SerialNumber"" <> upper(trim(""SerialNumber""));");

            // R4: sincroniza el estado de los equipos con sus asignaciones y mantenimientos.
            // EquipmentStatus: 0 AVAILABLE, 1 IN_USE, 2 UNDER_MAINTENANCE, 3 OUT_OF_SERVICE, 4 DECOMMISSIONED.
            // AssignmentStatus 0 = ACTIVE. MaintenanceStatus 0 = OPEN, 1 = IN_PROGRESS.
            migrationBuilder.Sql(@"
UPDATE ""Equipments"" e
SET ""Status"" = 2, ""UpdatedAt"" = now()
WHERE e.""Status"" IN (0, 1, 3)
  AND EXISTS (SELECT 1 FROM ""Maintenances"" m WHERE m.""EquipmentId"" = e.""Id"" AND m.""Status"" IN (0, 1));

UPDATE ""Equipments"" e
SET ""Status"" = CASE WHEN EXISTS (SELECT 1 FROM ""Assignments"" a WHERE a.""EquipmentId"" = e.""Id"" AND a.""Status"" = 0)
                      THEN 1 ELSE 0 END,
    ""UpdatedAt"" = now()
WHERE e.""Status"" IN (0, 1, 2)
  AND NOT EXISTS (SELECT 1 FROM ""Maintenances"" m WHERE m.""EquipmentId"" = e.""Id"" AND m.""Status"" IN (0, 1))
  AND e.""Status"" <> CASE WHEN EXISTS (SELECT 1 FROM ""Assignments"" a WHERE a.""EquipmentId"" = e.""Id"" AND a.""Status"" = 0)
                           THEN 1 ELSE 0 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "VoidedById",
                table: "Maintenances");
        }
    }
}
