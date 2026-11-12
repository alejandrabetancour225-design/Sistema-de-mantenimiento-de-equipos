-- Borra los datos que crea verificacion/fase0.sh (equipos QA-S6-* y su empleado de prueba).
BEGIN;
CREATE TEMP TABLE qa_eq AS SELECT "Id" FROM "Equipments" WHERE "InternalCode" LIKE 'QA-S6-%';
CREATE TEMP TABLE qa_us AS SELECT "Id" FROM "Users" WHERE "FullName" = 'QA S6 Empleado';
DELETE FROM "Notifications" WHERE "UserId" IN (SELECT "Id" FROM qa_us);
DELETE FROM "AuditLogs" WHERE "EntityId" IN (SELECT "Id"::text FROM qa_eq) OR "UserId" IN (SELECT "Id" FROM qa_us)
   OR ("EntityType" = 'Assignment' AND "EntityId" IN (SELECT "Id"::text FROM "Assignments" WHERE "EquipmentId" IN (SELECT "Id" FROM qa_eq)))
   OR ("EntityType" = 'User' AND "EntityId" IN (SELECT "Id"::text FROM qa_us));
DELETE FROM "Assignments" WHERE "EquipmentId" IN (SELECT "Id" FROM qa_eq);
DELETE FROM "Equipments" WHERE "Id" IN (SELECT "Id" FROM qa_eq);
DELETE FROM "Users" WHERE "Id" IN (SELECT "Id" FROM qa_us);
COMMIT;
