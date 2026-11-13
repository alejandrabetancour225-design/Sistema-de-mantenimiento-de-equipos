#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Verificación manual del alquiler de equipos (Sprint 6, fase 4) contra la API en marcha.
# Crea una organización de prueba "QA-S6 Alquiler <n>" con su administrador.
#
# Uso: SUPERADMIN_EMAIL=... SUPERADMIN_PASSWORD=... ./verificacion/fase4.sh
# ---------------------------------------------------------------------------
set -uo pipefail

API="${API_URL:-http://localhost:5255}/api"
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
expect() { if [[ "$CODE" == "$2" ]]; then pass "$1 ($CODE)"; else fail "$1" "esperaba $2, recibió $CODE: $BODY"; fi; }
json() { python3 -c "import sys,json; d=json.load(sys.stdin); print(eval(sys.argv[1]))" "$1" <<<"$BODY"; }
login_body() { curl -s -X POST "$API/auth/login" -H "Content-Type: application/json" -d "{\"email\":\"$1\",\"password\":\"$2\"}"; }

SA=$(login_body "$SUPERADMIN_EMAIL" "$SUPERADMIN_PASSWORD" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))")
[[ -z "$SA" ]] && { echo "No se pudo iniciar sesión como SuperAdmin."; exit 1; }
TEMP="Temporal${RUN}x"
req POST /organizations "$SA" "{\"name\":\"QA-S6 Alquiler $RUN\",\"plan\":\"BASICO\",\"administrator\":{\"fullName\":\"Admin Alquiler\",\"email\":\"admin.alq.$RUN@example.com\",\"role\":\"Administrador\",\"temporaryPassword\":\"$TEMP\"}}"
BODY=$(login_body "admin.alq.$RUN@example.com" "$TEMP"); T=$(json "d['token']")
req POST /auth/change-password "$T" "{\"currentPassword\":\"$TEMP\",\"newPassword\":\"Definitiva${RUN}x\"}"
T=$(json "d['token']"); ADMIN_ID=$(json "d['userId']")

TODAY=$(TZ=America/Bogota date +%F)
END=$(TZ=America/Bogota date -d '+20 days' +%F 2>/dev/null || TZ=America/Bogota date -v+20d +%F)

echo "== Arrendador y contrato"
req POST /rental/Lessors "$T" '{"name":"Rentacomputo S.A.S.","taxId":"900123456","contactName":"Ana Ruiz","contactEmail":"soporte@rentacomputo.example","active":true}'
expect "Crear arrendador" 200; LESSOR=$(json "d['id']")
req POST /rental/Contracts "$T" "{\"lessorId\":\"$LESSOR\",\"contractNumber\":\"RC-$RUN\",\"startDate\":\"$TODAY\",\"endDate\":\"$END\",\"allowsInternalPreventive\":false,\"rates\":[{\"equipmentType\":\"Portátil\",\"monthlyRate\":150000},{\"equipmentType\":\"Impresora\",\"monthlyRate\":80000}]}"
expect "Crear contrato con tarifas por tipo" 200; CONTRACT=$(json "d['id']")
req POST /rental/Contracts "$T" "{\"lessorId\":\"$LESSOR\",\"contractNumber\":\"RC-MAL-$RUN\",\"startDate\":\"$END\",\"endDate\":\"$TODAY\",\"rates\":[{\"equipmentType\":\"Portátil\",\"monthlyRate\":1}]}"
expect "Fin antes del inicio se rechaza" 400

echo "== Equipo alquilado"
BASE="\"serialNumber\":\"SN-ALQ-$RUN\",\"brand\":\"HP\",\"model\":\"240\",\"location\":\"Piso 2\",\"ownership\":\"RENTED\",\"rentalContractId\":\"$CONTRACT\""
req POST /equipment/PostNewEquipment "$T" "{\"internalCode\":\"ALQ-1\",\"type\":\"Monitor\",$BASE}"
expect "Tipo sin tarifa en el contrato se rechaza" 400
req POST /equipment/PostNewEquipment "$T" "{\"internalCode\":\"ALQ-1\",\"type\":\"Portátil\",$BASE,\"acquisitionPrice\":999}"
expect "Registrar portátil alquilado" 200; EQ=$(json "d['id']")
[[ "$(json "d['acquisitionDate']")" == "$TODAY" && "$(json "d['acquisitionPrice']")" == "None" ]] \
  && pass "Fecha = inicio del contrato y sin precio de compra" || fail "Fechas del alquilado" "$BODY"
[[ "$(json "d['lessorName']")" == "Rentacomputo S.A.S." ]] && pass "El equipo muestra su arrendador" || fail "lessorName" "$BODY"

echo "== Mantenimiento y reclamos"
req POST /maintenance/PostNewMaintenance "$T" "{\"equipmentId\":\"$EQ\",\"technicianId\":\"$ADMIN_ID\",\"type\":\"CORRECTIVE\",\"reportedProblem\":\"No prende\"}"
expect "Correctivo interno en alquilado se rechaza (va al arrendador)" 400
req POST /maintenance/PostNewMaintenance "$T" "{\"equipmentId\":\"$EQ\",\"technicianId\":\"$ADMIN_ID\",\"type\":\"PREVENTIVE\",\"reportedProblem\":\"Limpieza\"}"
expect "Preventivo interno no permitido por el contrato" 400
req POST /rental/Claims "$T" "{\"equipmentId\":\"$EQ\",\"description\":\"El portátil no enciende desde esta mañana\"}"
expect "Reclamo al arrendador" 200; CLAIM=$(json "d['id']")
req PUT "/rental/Claims/$CLAIM" "$T" '{"status":"RESOLVED","lessorResponse":"Se enviará un reemplazo"}'
expect "Marcar reclamo resuelto" 200

echo "== Costo, baja y devolución"
req GET /rental/Contracts "$T"
[[ "$(json "[c for c in d if c['id']=='$CONTRACT'][0]['monthlyCost']")" == "150000.0" || "$(json "[c for c in d if c['id']=='$CONTRACT'][0]['monthlyCost']")" == "150000" ]] \
  && pass "Costo mensual = tarifa del portátil" || fail "Costo mensual" "$BODY"
req GET /rental/Contracts/Expiring "$T"
[[ "$(json "any(c['id']=='$CONTRACT' for c in d)")" == "True" ]] && pass "El contrato aparece por vencer (20 días)" || fail "Por vencer" "$BODY"
req DELETE "/equipment/DeleteEquipment/$EQ" "$T"; expect "Un alquilado no se da de baja" 409
req POST "/rental/Equipment/$EQ/Return" "$T"; expect "Devolver al arrendador" 200
req POST "/rental/Equipment/$EQ/Return" "$T"; expect "No se devuelve dos veces" 409
req PATCH "/equipment/PatchEquipmentStatus/$EQ" "$T" '{"status":"AVAILABLE"}'; expect "Devuelto es terminal" 400
req POST "/rental/Equipment/$EQ/Replacement" "$T" "{\"internalCode\":\"ALQ-2\",\"serialNumber\":\"SN-ALQ2-$RUN\",\"brand\":\"HP\",\"model\":\"250\"}"
expect "Registrar equipo de reemplazo" 200; REP=$(json "d['id']")
req GET "/equipment/GetEquipmentById/$REP" "$T"
[[ "$(json "d['replacesEquipmentId']")" == "$EQ" && "$(json "d['status']")" == "AVAILABLE" ]] \
  && pass "El reemplazo queda vinculado y Disponible" || fail "Reemplazo" "$BODY"

echo
echo "Resultado: $OK OK, $FAIL fallas.  Organización de prueba: 'QA-S6 Alquiler $RUN'"
[[ $FAIL -eq 0 ]]
