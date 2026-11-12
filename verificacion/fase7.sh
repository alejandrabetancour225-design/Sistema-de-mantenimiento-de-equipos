#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Verificación manual de reportes con IA e información de mercado (Sprint 6, fase 7).
# Crea organizaciones "QA-S6 Reportes <n>" (Integraciones) y una Básica. Tarda unos minutos
# porque la IA redacta el reporte en CPU.
#
# Uso: SUPERADMIN_EMAIL=... SUPERADMIN_PASSWORD=... ./verificacion/fase7.sh
# ---------------------------------------------------------------------------
set -uo pipefail
cd "$(dirname "$0")/.."

API="${API_URL:-http://localhost:5255}/api"
RUN=$(date +%s)
OK=0; FAIL=0
pass() { echo "  OK    $1"; OK=$((OK+1)); }
fail() { echo "  FALLA $1"; echo "        -> $2"; FAIL=$((FAIL+1)); }
req() {
  local method=$1 path=$2 token=$3 data=${4:-} out
  if [[ -n "$data" ]]; then
    out=$(curl -s -m 200 -w '\n%{http_code}' -X "$method" "$API$path" -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d "$data")
  else
    out=$(curl -s -m 200 -w '\n%{http_code}' -X "$method" "$API$path" -H "Authorization: Bearer $token")
  fi
  CODE=$(tail -n1 <<<"$out"); BODY=$(sed '$d' <<<"$out")
}
expect() { if [[ "$CODE" == "$2" ]]; then pass "$1 ($CODE)"; else fail "$1" "esperaba $2, recibió $CODE: ${BODY:0:300}"; fi; }
json() { python3 -c "import sys,json; d=json.load(sys.stdin); print(eval(sys.argv[1]))" "$1" <<<"$BODY"; }
login_body() { curl -s -X POST "$API/auth/login" -H "Content-Type: application/json" -d "{\"email\":\"$1\",\"password\":\"$2\"}"; }

SA=$(login_body "$SUPERADMIN_EMAIL" "$SUPERADMIN_PASSWORD" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))")
[[ -z "$SA" ]] && { echo "No se pudo iniciar sesión como SuperAdmin."; exit 1; }
TEMP="Temporal${RUN}x"
org_admin() {
  req POST /organizations "$SA" "{\"name\":\"QA-S6 Reportes $1 $RUN\",\"plan\":\"$2\",\"administrator\":{\"fullName\":\"Admin $1\",\"email\":\"admin.rep.$1.$RUN@example.com\",\"role\":\"Administrador\",\"temporaryPassword\":\"$TEMP\"}}"
  local t; t=$(login_body "admin.rep.$1.$RUN@example.com" "$TEMP" | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")
  curl -s -X POST "$API/auth/change-password" -H "Authorization: Bearer $t" -H "Content-Type: application/json" \
    -d "{\"currentPassword\":\"$TEMP\",\"newPassword\":\"Definitiva${RUN}x\"}" | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])"
}
T=$(org_admin full INTEGRACIONES)
TB=$(org_admin basico BASICO)

echo "== Plan"
req POST /aireports/Generate "$TB" '{}'; expect "El plan Básico no genera reportes con IA" 403

echo "== Datos del periodo"
for i in 1 2 3; do
  req POST /equipment/PostNewEquipment "$T" "{\"internalCode\":\"REP-$i\",\"serialNumber\":\"SN-REP-$i\",\"type\":\"Portátil\",\"brand\":\"Lenovo\",\"model\":\"ThinkPad T14\",\"location\":\"Bogotá\",\"acquisitionDate\":\"2020-02-01\",\"acquisitionPrice\":3500000}"
done
EQ=$(json "d['id']"); ADMIN_ID=$(curl -s "$API/auth/me" -H "Authorization: Bearer $T" | python3 -c "import sys,json; print(json.load(sys.stdin)['userId'])")
req POST /maintenance/PostNewMaintenance "$T" "{\"equipmentId\":\"$EQ\",\"technicianId\":\"$ADMIN_ID\",\"type\":\"CORRECTIVE\",\"reportedProblem\":\"Se apaga solo\",\"laborCost\":120000}"
MNT=$(json "d['id']")
req POST "/maintenance/CloseMaintenance/$MNT" "$T" '{"workDone":"Cambio de ventilador y pasta térmica","otherCosts":80000}'
expect "Mantenimiento correctivo cerrado (con costos)" 200

echo "== Mercado"
req GET "/market/Equipment/$EQ" "$T"
expect "Buscar precio y guías del equipo" 200
if [[ "$(json "d['available']")" == "True" ]]; then
  pass "Resultados con fuente y fecha: $(json "len(d['results'])") (ej.: $(json "d['results'][0]['url'][:60]"))"
else
  echo "  AVISO buscadores sin respuesta en este momento (el reporte se genera igual)"
fi

echo "== Reporte ejecutivo"
req POST /aireports/Generate "$T" '{}'; expect "Pedir reporte" 202
REP=$(json "d['id']")
req POST /aireports/Generate "$T" '{}'; expect "No se piden dos a la vez" 409
for i in $(seq 1 60); do
  req GET "/aireports/$REP" "$T"
  [[ "$(json "d['status']")" != "PENDING" ]] && break
  sleep 5
done
[[ "$(json "d['status']")" == "DONE" ]] && pass "Reporte generado" || fail "Reporte" "${BODY:0:400}"
if [[ "$(json "d['status']")" == "DONE" ]]; then
  echo "        Resumen: $(json "d['content']['summary'][:220]")"
  python3 - "$BODY" <<'PY' && pass "Las notas de mercado solo citan fuentes consultadas" || fail "Fuentes del reporte" "cita una URL no consultada"
import json, sys
d = json.loads(sys.argv[1])
urls = {r["url"] for r in ((d.get("market") or {}).get("results") or [])}
sys.exit(0 if all(n["url"] in urls for n in d["content"]["market_notes"]) else 1)
PY
  ES=$(python3 -c "
import json,sys,unicodedata
d=json.loads(sys.argv[1])['content']
t=' '.join([d['summary']]+d['findings']+[r['text'] for r in d['recommendations']]).lower()
es=sum(w in t.split() for w in ['de','la','el','que','en','los','del','se','para','con','equipos','mantenimiento'])
en=sum(w in t.split() for w in ['the','of','and','to','is','for','with','equipment','maintenance'])
print('si' if es>3*max(en,1) else 'no')" "$BODY")
  [[ "$ES" == "si" ]] && pass "Redactado en español" || fail "Idioma del reporte" "parece no estar en español"
  CODE=$(curl -s -o /tmp/qa-reporte.pdf -w '%{http_code}' "$API/aireports/$REP/Pdf" -H "Authorization: Bearer $T")
  [[ "$CODE" == "200" && "$(head -c 4 /tmp/qa-reporte.pdf)" == "%PDF" ]] && pass "PDF descargable ($(wc -c < /tmp/qa-reporte.pdf) bytes)" || fail "PDF" "$CODE"
  req GET /notifications "$T"
  [[ "$(json "any('Reporte ejecutivo' in n['title'] for n in d['items'])")" == "True" ]] && pass "Aviso en la campana" || fail "Notificación" "$BODY"
fi

echo
echo "Resultado: $OK OK, $FAIL fallas."
[[ $FAIL -eq 0 ]]
