#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Verificación manual del calendario, recordatorios y tiempo real (Sprint 6, fase 5).
# Crea una organización de prueba "QA-S6 Calendario <n>". Necesita Node (usa @microsoft/signalr
# de Frontend/node_modules para comprobar el canal en tiempo real).
#
# Uso: SUPERADMIN_EMAIL=... SUPERADMIN_PASSWORD=... ./verificacion/fase5.sh
# ---------------------------------------------------------------------------
set -uo pipefail
cd "$(dirname "$0")/.."

API_BASE="${API_URL:-http://localhost:5255}"
API="$API_BASE/api"
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
day() { TZ=America/Bogota date -d "$1" +%F 2>/dev/null || TZ=America/Bogota date -v"$2" +%F; }

SA=$(login_body "$SUPERADMIN_EMAIL" "$SUPERADMIN_PASSWORD" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))")
[[ -z "$SA" ]] && { echo "No se pudo iniciar sesión como SuperAdmin."; exit 1; }
TEMP="Temporal${RUN}x"
req POST /organizations "$SA" "{\"name\":\"QA-S6 Calendario $RUN\",\"plan\":\"BASICO\",\"administrator\":{\"fullName\":\"Admin Cal\",\"email\":\"admin.cal.$RUN@example.com\",\"role\":\"Administrador\",\"temporaryPassword\":\"$TEMP\"}}"
BODY=$(login_body "admin.cal.$RUN@example.com" "$TEMP"); T=$(json "d['token']")
req POST /auth/change-password "$T" "{\"currentPassword\":\"$TEMP\",\"newPassword\":\"Definitiva${RUN}x\"}"
T=$(json "d['token']"); ADMIN_ID=$(json "d['userId']")

TODAY=$(TZ=America/Bogota date +%F)
IN5=$(day '+5 days' +5d); MONTH_END=$(day '+40 days' +40d)

echo "== Tiempo real (SignalR)"
EVENTS=$(mktemp)
API_BASE="$API_BASE" TOKEN="$T" node -e '
const { HubConnectionBuilder } = require("./Frontend/node_modules/@microsoft/signalr");
const c = new HubConnectionBuilder().withUrl(process.env.API_BASE + "/api/hubs/notificaciones", { accessTokenFactory: () => process.env.TOKEN }).build();
c.on("datosCambiaron", p => console.log("datosCambiaron " + p));
c.on("notificacion", n => console.log("notificacion " + n.title));
c.start().then(() => console.log("conectado")).catch(e => { console.log("error " + e); process.exit(1); });
setTimeout(() => process.exit(0), 12000);
' > "$EVENTS" 2>&1 &
NODE_PID=$!
sleep 3
grep -q "^conectado" "$EVENTS" && pass "Conexión al hub con el token" || fail "Conexión SignalR" "$(cat "$EVENTS")"

echo "== Plan preventivo y calendario"
req POST /equipment/PostNewEquipment "$T" '{"internalCode":"CAL-1","serialNumber":"SN-CAL-1","type":"Portátil","brand":"Lenovo","model":"T14","location":"Piso 1"}'
expect "Registrar equipo" 200; EQ=$(json "d['id']")
req POST /maintenanceplans "$T" "{\"equipmentId\":\"$EQ\",\"description\":\"Limpieza y revisión\",\"intervalWeeks\":4,\"nextDate\":\"$(day '-2 days' -2d)\",\"active\":true}"
expect "Una fecha pasada se rechaza" 400
req POST /maintenanceplans "$T" "{\"equipmentId\":\"$EQ\",\"technicianId\":\"$ADMIN_ID\",\"description\":\"Limpieza y revisión\",\"intervalWeeks\":4,\"nextDate\":\"$IN5\",\"active\":true}"
expect "Programar cada 4 semanas" 200; PLAN=$(json "d['id']")
sleep 1
grep -q "^datosCambiaron" "$EVENTS" && pass "Los cambios se avisan en tiempo real a la organización" || fail "Aviso datosCambiaron" "$(cat "$EVENTS")"

req GET "/calendar?from=$TODAY&to=$MONTH_END" "$T"
N=$(json "len([e for e in d if e['kind']=='plan'])")
[[ "$N" == "2" ]] && pass "El calendario muestra la fecha y su repetición (2 en 40 días)" || fail "Eventos del plan" "$N: $BODY"

echo "== Recordatorios"
wait $NODE_PID 2>/dev/null
req POST /notifications/RunReminders "$T"; expect "Pedir revisión de recordatorios" 202
for i in $(seq 1 30); do
  req GET /notifications "$T"
  [[ "$(json "d['unread']")" != "0" ]] && break
  sleep 2
done
[[ "$(json "d['unread']")" != "0" ]] && pass "Aviso de 7 días en la campana: $(json "d['items'][0]['title']")" \
  || fail "Recordatorio" "Sin notificaciones tras 60 s (¿Hangfire activo? se ejecuta al arrancar y cada hora, minuto 15)"

echo "== Iniciar y cerrar desde el plan"
req POST "/maintenanceplans/$PLAN/Start" "$T"; expect "Iniciar mantenimiento programado" 200; MNT=$(json "d['id']")
req POST "/maintenance/CloseMaintenance/$MNT" "$T" '{"workDone":"Limpieza interna y revisión general"}'; expect "Cerrar el preventivo" 200
req GET "/maintenanceplans?equipmentId=$EQ" "$T"
EXPECTED=$(day '+28 days' +28d)
[[ "$(json "d[0]['nextDate']")" == "$EXPECTED" ]] && pass "La próxima fecha se calculó sola (hoy + 4 semanas = $EXPECTED)" \
  || fail "Fecha automática" "$(json "d[0]['nextDate']") != $EXPECTED"
req PATCH "/maintenanceplans/$PLAN/Reschedule" "$T" "{\"nextDate\":\"$(day '+30 days' +30d)\"}"; expect "Reprogramar (arrastrar)" 200

echo
echo "Resultado: $OK OK, $FAIL fallas.  Organización de prueba: 'QA-S6 Calendario $RUN'"
rm -f "$EVENTS"
[[ $FAIL -eq 0 ]]
