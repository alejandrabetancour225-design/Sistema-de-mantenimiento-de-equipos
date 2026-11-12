-- Base "de un tercero" para probar integraciones (verificacion/fase6.sh), en el Postgres local:
--   docker exec -i sm-postgres psql -U postgres < verificacion/base-externa.sql
DROP DATABASE IF EXISTS erp_externo;
CREATE DATABASE erp_externo;
\c erp_externo
CREATE TABLE activos (
  placa text PRIMARY KEY, nro_serie text, clase text, fabricante text, referencia text,
  sede text, fecha_compra date, valor_compra numeric(14,2), responsable text, centro_costo text);
INSERT INTO activos VALUES
 ('ACT-1001','5CG1234XYZ','Portátil','HP','ProBook 440 G8','Bogotá - Torre A','2023-02-15',3800000,'Laura Gómez','CC-110'),
 ('ACT-1002','PF3K9L2M','Portátil','Lenovo','ThinkPad E14','Medellín','2022-08-01',3200000,'Andrés Rojas','CC-120'),
 ('ACT-1003','CN0D4F7G','Monitor','Dell','P2422H','Bogotá - Torre A','2024-01-10',950000,NULL,'CC-110'),
 ('ACT-1004','X8Y7Z6W5','Impresora','Epson','L3250','Cali','2030-01-01',780000,NULL,'CC-130');
-- Usuario de SOLO LECTURA, como se recomienda a las empresas.
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='lector') THEN CREATE ROLE lector LOGIN PASSWORD 'LectorSolo2026'; END IF; END $$;
GRANT CONNECT ON DATABASE erp_externo TO lector;
GRANT SELECT ON activos TO lector;
