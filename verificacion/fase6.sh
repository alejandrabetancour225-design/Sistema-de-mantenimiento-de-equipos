#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Verificación manual de integraciones con bases de terceros (Sprint 6, fase 6).
# Crea una organización "QA-S6 Integraciones <n>" (plan Integraciones) y otra Básica.
#
# Requiere la base externa de prueba (ver verificacion/base-externa.sql) en el Postgres
# local (./iniciar.sh --db-local); python-service la alcanza como host "postgres".
#
# Uso: SUPERADMIN_EMAIL=... SUPERADMIN_PASSWORD=... ./verificacion/fase6.sh
# ---------------------------------------------------------------------------
set -uo pipefail
cd "$(dirname "$0")/.."

API="${API_URL:-http://localhost:5255}/api"
EXT_HOST="${EXT_HOST:-postgres}"
RUN=$(date +%s)
OK=0; FAIL=0
pass() { echo "  OK    $1"; OK=$((OK+1)); }
fail() { echo "  FALLA $1"; echo "        -> $2"; FAIL=$((FAIL+1)); }
req() {
  local method=$1 path=$2 token=$3 data=${4:-} out
  if [[ -n "$data" ]]; then
    out=$(curl -s -w '\n%{http_code}' -X "$method" "$API$path" -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d "$data")
  else
    out=$(curl -s -w '\n%{http_code}' -X "$method" "$API$path" -H "Authorization: Bearer $token")
  fi
  CODE=$(tail -n1 <<<"$out"); BODY=$(sed '$d' <<<"$out")
}
expect() { if [[ "$CODE" == "$2" ]]; then pass "$1 ($CODE)"; else fail "$1" "esperaba $2, recibió $CODE: ${BODY:0:300}"; fi; }
json() { python3 -c "import sys,json; d=json.load(sys.stdin); print(eval(sys.argv[1]))" "$1" <<<"$BODY"; }
login_body() { curl -s -X POST "$API/auth/login" -H "Content-Type: application/json" -d "{\"email\":\"$1\",\"password\":\"$2\"}"; }

SA=$(login_body "$SUPERADMIN_EMAIL" "$SUPERADMIN_PASSWORD" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))")
[[ -z "$SA" ]] && { echo "No se pudo iniciar sesión como SuperAdmin."; exit 1; }
TEMP="Temporal${RUN}x"
org_admin() { # org_admin <sufijo> <plan> -> token
  req POST /organizations "$SA" "{\"name\":\"QA-S6 Integraciones $1 $RUN\",\"plan\":\"$2\",\"administrator\":{\"fullName\":\"Admin $1\",\"email\":\"admin.int.$1.$RUN@example.com\",\"role\":\"Administrador\",\"temporaryPassword\":\"$TEMP\"}}"
  local t; t=$(login_body "admin.int.$1.$RUN@example.com" "$TEMP" | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")
  curl -s -X POST "$API/auth/change-password" -H "Authorization: Bearer $t" -H "Content-Type: application/json" \
    -d "{\"currentPassword\":\"$TEMP\",\"newPassword\":\"Definitiva${RUN}x\"}" | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])"
}
T=$(org_admin full INTEGRACIONES)
TB=$(org_admin basico BASICO)

echo "== Plan"
req POST /integrations/Profile "$TB" '{"kind":"file","fileName":"a.csv","fileBase64":"YQ=="}'
expect "El plan Básico no accede a integraciones" 403

echo "== Archivo CSV (separador ;, fechas dd/mm/aaaa, precios 1.234.567)"
CSV=$'Placa;Número de serie;Tipo equipo;Marca;Modelo;Sede;Fecha compra;Valor compra;Responsable\nINV-001;SN9A8B7C;Portátil;Lenovo;ThinkPad T14;Bogotá;15/03/2023;4.200.000;Laura\nINV-002;SN1D2E3F;Monitor;Samsung;S24R350;Medellín;01/08/2022;650.000;Andrés\nINV-003;SN4G5H6I;Impresora;Epson;L3250;Cali;10/01/2024;900.000;'
B64=$(printf '%s' "$CSV" | base64 -w0 2>/dev/null || printf '%s' "$CSV" | base64)
req POST /integrations/Profile "$T" "{\"kind\":\"file\",\"fileName\":\"inventario.csv\",\"fileBase64\":\"$B64\"}"
expect "Perfilar el CSV" 200
S=$(json "{k: v['column'] for k, v in d['profile']['suggestion']['fields'].items()}")
[[ "$(json "d['profile']['suggestion']['fields']['internalCode']['column']")" == "Placa" \
   && "$(json "d['profile']['suggestion']['fields']['serialNumber']['column']")" == "Número de serie" \
   && "$(json "d['profile']['suggestion']['fields']['acquisitionPrice']['column']")" == "Valor compra" ]] \
  && pass "Mapeo sugerido correcto: $S" || fail "Mapeo sugerido" "$S"
[[ "$(json "[c['type'] for c in d['profile']['columns'] if c['column']=='Fecha compra'][0]")" == "fecha" ]] \
  && pass "Detecta la columna de fechas" || fail "Tipo fecha" "$BODY"
MAP='{"internalCode":"Placa","serialNumber":"Número de serie","type":"Tipo equipo","brand":"Marca","model":"Modelo","location":"Sede","acquisitionDate":"Fecha compra","acquisitionPrice":"Valor compra"}'
req POST /integrations/Sources "$T" "{\"name\":\"Inventario Excel\",\"kind\":\"file\",\"fileName\":\"inventario.csv\",\"fileBase64\":\"$B64\",\"mapping\":$MAP,\"keyColumn\":\"Placa\"}"
expect "Guardar e importar" 200
[[ "$(json "d['summary']['created']")" == "3" ]] && pass "3 equipos importados" || fail "Importación" "$BODY"
req GET /equipment/GetEquipmentList "$T"
EQ=$(json "[e['id'] for e in d if e['internalCode']=='INV-001'][0]")
[[ "$(json "float([e['acquisitionPrice'] for e in d if e['internalCode']=='INV-001'][0])")" == "4200000.0" ]] \
  && pass "Precio colombiano 4.200.000 interpretado" || fail "Precio" "$(json "[e for e in d if e['internalCode']=='INV-001']")"
req GET "/equipment/GetEquipmentById/$EQ" "$T"
[[ "$(json "d['externalAttributes']['Responsable']")" == "Laura" ]] && pass "Columna sin mapear guardada como atributo (JSONB)" || fail "Atributos" "$BODY"
[[ "$(json "'brand' in d['lockedFields']")" == "True" ]] && pass "Campos mapeados bloqueados" || fail "lockedFields" "$BODY"
req PUT "/equipment/PutEquipment/$EQ" "$T" '{"internalCode":"INV-001","serialNumber":"SN9A8B7C","type":"Portátil","brand":"OTRA","model":"ThinkPad T14","location":"Bogotá","acquisitionDate":"2023-03-15","acquisitionPrice":4200000}'
expect "No se edita un campo sincronizado" 409

echo "== Conexión en vivo a PostgreSQL (usuario de solo lectura)"
CONN="\"kind\":\"postgres\",\"host\":\"$EXT_HOST\",\"port\":5432,\"database\":\"erp_externo\",\"user\":\"lector\",\"password\":\"LectorSolo2026\",\"sslMode\":\"disable\",\"table\":\"activos\""
req POST /integrations/Profile "$T" "{$CONN}"; expect "Perfilar la tabla externa" 200
[[ "$(json "d['profile']['suggestion']['fields']['internalCode']['column']")" == "placa" \
   && "$(json "d['profile']['suggestion']['fields']['brand']['column']")" == "fabricante" ]] \
  && pass "Mapeo sugerido en la base externa (placa, fabricante…)" || fail "Mapeo externo" "$(json "d['profile']['suggestion']")"
req POST /integrations/Profile "$T" "{\"kind\":\"postgres\",\"host\":\"$EXT_HOST\",\"database\":\"erp_externo\",\"user\":\"lector\",\"password\":\"LectorSolo2026\",\"table\":\"activos; DROP TABLE activos\"}"
expect "Nombre de tabla con inyección se rechaza" 400
MAP2='{"internalCode":"placa","serialNumber":"nro_serie","type":"clase","brand":"fabricante","model":"referencia","location":"sede","acquisitionDate":"fecha_compra","acquisitionPrice":"valor_compra"}'
req POST /integrations/Sources "$T" "{\"name\":\"ERP externo\",$CONN,\"mapping\":$MAP2,\"keyColumn\":\"placa\",\"syncIntervalHours\":24}"
expect "Conectar e importar" 200
[[ "$(json "d['summary']['created']")" == "4" ]] && pass "4 activos importados" || fail "Importación externa" "$BODY"
[[ "$(json "any('futura' in e for e in d['summary']['errors'])")" == "True" ]] && pass "Fecha de compra futura (2030) detectada y no importada" || fail "Fecha futura" "$BODY"
SRC=$(json "d['id']")
req POST "/integrations/Sources/$SRC/Sync" "$T"; expect "Sincronizar de nuevo" 200
[[ "$(json "d['created']")" == "0" && "$(json "d['updated']")" == "4" ]] && pass "La segunda vez actualiza, no duplica" || fail "Resincronizar" "$BODY"

echo
echo "Resultado: $OK OK, $FAIL fallas."
[[ $FAIL -eq 0 ]]
