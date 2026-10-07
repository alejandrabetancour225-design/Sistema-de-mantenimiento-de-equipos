-- Usuario de la aplicación con permisos mínimos (#13).
-- La API debe conectarse con este usuario, NO con el dueño de la base (neondb_owner / postgres).
-- Las migraciones se aplican aparte, con el usuario dueño:  dotnet ef database update
--
-- Ejecutar conectado como dueño de la base. Cambia la contraseña antes de ejecutarlo
-- y guárdala solo en user-secrets o variables de entorno (Database__User / Database__Password).

CREATE ROLE app_user WITH LOGIN PASSWORD 'CAMBIA-ESTA-CONTRASEÑA';

DO $$
BEGIN
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO app_user', current_database());
END
$$;

GRANT USAGE ON SCHEMA public TO app_user;

-- Solo datos: leer y escribir filas. Sin CREATE, ALTER, DROP ni TRUNCATE.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO app_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO app_user;

-- Tablas creadas por migraciones futuras (ejecutadas por el dueño) heredan los mismos permisos.
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO app_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO app_user;

-- La auditoría solo se agrega: la aplicación no puede modificar ni borrar eventos.
REVOKE UPDATE, DELETE ON "AuditLogs" FROM app_user;

-- El historial de migraciones solo lo toca el dueño.
REVOKE INSERT, UPDATE, DELETE ON "__EFMigrationsHistory" FROM app_user;
