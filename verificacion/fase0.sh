#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Verificación manual de las reglas de la fase 0 (Sprint 6) contra la API en marcha.
# No es una suite de pruebas: ejecuta peticiones reales y muestra OK/FALLA por caso.
#
# Uso:  ADMIN_EMAIL=... ADMIN_PASSWORD=... ./verificacion/fase0.sh
# Crea un equipo y un empleado de prueba con prefijo QA-S6 (el equipo queda dado de baja al final).
# ---------------------------------------------------------------------------
set -uo pipefail

API="${API_URL:-http://localhost:5255}/api"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@test.com}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-AdminSegura123}"
RUN=$(date +%s)
OK=0; FAIL=0

pass() { echo "  OK    $1"; OK=$((OK+1)); }
fail() { echo "  FALLA $1"; echo "        -> $2"; FAIL=$((FAIL+1)); }

# req METHOD PATH TOKEN [BODY]  -> deja el código en $CODE y el cuerpo en $BODY
req() {
  local method=$1 path=$2 token=$3 data=${4:-}
  local out
  if [[ -n "$data" ]]; then
    out=$(curl -s -w '\n%{http_code}' -X "$method" "$API$path" -H "Authorization: Bearer $token" \
      -H "X-CSRF: 1" -H "Content-Type: application/json" -d "$data")
  else
    out=$(curl -s -w '\n%{http_code}' -X "$method" "$API$path" -H "Authorization: Bearer $token" -H "X-CSRF: 1")
  fi
  CODE=$(tail -n1 <<<"$out"); BODY=$(sed '$d' <<<"$out")
}

expect() { # expect "descripción" código_esperado
  if [[ "$CODE" == "$2" ]]; then pass "$1 ($CODE)"; else fail "$1" "esperaba $2, recibió $CODE: $BODY"; fi
}

json() { python3 -c "import sys,json; d=json.load(sys.stdin); print(eval(sys.argv[1]))" "$1" <<<"$BODY"; }

login() { # login email password -> token
  curl -s -X POST "$API/auth/login" -H "Content-Type: application/json" \
    -d "{\"email\":\"$1\",\"password\":\"$2\"}" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))"
}

TODAY=$(TZ=America/Bogota date +%F)
TOMORROW=$(TZ=America/Bogota date -d tomorrow +%F 2>/dev/null || TZ=America/Bogota date -v+1d +%F)

ADMIN=$(login "$ADMIN_EMAIL" "$ADMIN_PASSWORD")
[[ -z "$ADMIN" ]] && { echo "No se pudo iniciar sesión como Administrador."; exit 1; }

echo "== Fechas del equipo (Q9)"
CODE_LOWER="qa-s6-$RUN"
base() { echo "\"internalCode\":\"$1\",\"serialNumber\":\"$2\",\"type\":\"Laptop\",\"brand\":\"Lenovo\",\"model\":\"T14\",\"location\":\"Bodega QA\""; }

req POST /equipment/PostNewEquipment "$ADMIN" "{$(base "$CODE_LOWER" "sn-$RUN"),\"acquisitionDate\":\"$TOMORROW\"}"
expect "Fecha de adquisición futura se rechaza" 400

req POST /equipment/PostNewEquipment "$ADMIN" "{$(base "$CODE_LOWER" "sn-$RUN"),\"acquisitionDate\":\"2025-06-10\",\"warrantyUntil\":\"2025-01-01\"}"
expect "Garantía anterior a la compra se rechaza" 400

req POST /equipment/PostNewEquipment "$ADMIN" "{$(base "$CODE_LOWER" "sn-$RUN"),\"acquisitionDate\":\"$TODAY\",\"warrantyUntil\":\"2027-01-01\",\"acquisitionPrice\":3500000}"
expect "Equipo válido (compra hoy) se registra" 200
EQ=$(json "d['id']")
[[ "$(json "d['internalCode']")" == "QA-S6-$RUN" ]] && pass "Código guardado en mayúsculas" || fail "Código en mayúsculas" "$BODY"

req POST /equipment/PostNewEquipment "$ADMIN" "{$(base "QA-S6-$RUN" "otra-$RUN")}"
expect "Mismo código con otras mayúsculas es duplicado" 409

echo "== Corrección de datos (Q10)"
req PUT "/equipment/PutEquipment/$EQ" "$ADMIN" "{$(base "QA-S6-$RUN" "SN-$RUN"),\"acquisitionDate\":\"$TODAY\",\"warrantyUntil\":null,\"acquisitionPrice\":null}"
expect "Corregir: vaciar precio y garantía" 200
[[ "$(json "d['acquisitionPrice']")" == "None" ]] && pass "El precio quedó vacío" || fail "Precio vacío" "$BODY"

req PUT "/equipment/PutEquipment/$EQ" "$ADMIN" "{$(base "QA-S6-$RUN" "SN-$RUN"),\"acquisitionDate\":\"$TOMORROW\"}"
expect "Corregir con fecha futura se rechaza" 400

req GET "/audit?entityType=Equipment&entityId=$EQ&take=5" "$ADMIN"
grep -q '\\"old\\"' <<<"$BODY" && pass "Auditoría guarda valor anterior y nuevo" || fail "Auditoría con valores" "$BODY"

echo "== Matriz de estados R4 (Q17)"
req PATCH "/equipment/PatchEquipmentStatus/$EQ" "$ADMIN" '{"status":"UNDER_MAINTENANCE"}'
expect "No se pone En mantenimiento a mano" 400
req PATCH "/equipment/PatchEquipmentStatus/$EQ" "$ADMIN" '{"status":"OUT_OF_SERVICE"}'
expect "Marcar Fuera de servicio" 200
req PATCH "/equipment/PatchEquipmentStatus/$EQ" "$ADMIN" '{"status":"AVAILABLE"}'
expect "Devolver a servicio" 200

# Usuarios de prueba: el Administrador los crea con contraseña temporal (no hay registro público).
EMP_EMAIL="qa.s6.$RUN@example.com"; EMP_PASS="QaSprint6$RUN"; GER_EMAIL="qa.s6.ger.$RUN@example.com"
TEMP="Temporal${RUN}x"
new_user() { # new_user email rol -> token con la contraseña definitiva
  req POST /users "$ADMIN" "{\"fullName\":\"QA S6 Empleado\",\"email\":\"$1\",\"role\":\"$2\",\"temporaryPassword\":\"$TEMP\"}"
  local t; t=$(login "$1" "$TEMP")
  curl -s -X POST "$API/auth/change-password" -H "Authorization: Bearer $t" -H "Content-Type: application/json" \
    -d "{\"currentPassword\":\"$TEMP\",\"newPassword\":\"$EMP_PASS\"}" >/dev/null
}
new_user "$GER_EMAIL" "Gerente"; expect "El Administrador crea un Gerente" 200
GER_ID=$(json "d['id']")
new_user "$EMP_EMAIL" "Empleado"; expect "El Administrador crea un Empleado" 200
EMP_ID=$(json "d['id']")

req POST /assignment/AssignEquipment "$ADMIN" "{\"equipmentId\":\"$EQ\",\"userId\":\"$GER_ID\"}"
expect "No se asigna a un Gerente" 400

GER=$(login "$GER_EMAIL" "$EMP_PASS")
req GET /errorknowledge/GetKnowledgeList "$GER"
expect "El Gerente no consulta la biblioteca de errores" 403

req POST /assignment/AssignEquipment "$ADMIN" "{\"equipmentId\":\"$EQ\",\"userId\":\"$EMP_ID\"}"
expect "Asignar a Empleado" 200
ASG=$(json "d['id']")
req GET "/equipment/GetEquipmentById/$EQ" "$ADMIN"
[[ "$(json "d['status']")" == "IN_USE" ]] && pass "Asignar deja el equipo En uso" || fail "Estado tras asignar" "$BODY"

echo "== Visibilidad del Empleado (Q18)"
EMP=$(login "$EMP_EMAIL" "$EMP_PASS")
req GET /equipment/GetEquipmentList "$EMP"
[[ "$(json "len(d)")" == "1" && "$(json "d[0]['id']")" == "$EQ" ]] && pass "El Empleado solo ve su equipo" || fail "Lista del Empleado" "$(json "len(d)") equipos"
OTHER=$(curl -s "$API/equipment/GetEquipmentList" -H "Authorization: Bearer $ADMIN" | python3 -c "import sys,json; print([e['id'] for e in json.load(sys.stdin) if e['id']!='$EQ'][0])")
req GET "/equipment/GetEquipmentById/$OTHER" "$EMP"
expect "El Empleado no abre un equipo ajeno" 404
req GET "/maintenance/GetHistoryByEquipment/$OTHER" "$EMP"
expect "El Empleado no ve el historial de un equipo ajeno" 404
req GET /errorknowledge/GetKnowledgeList "$EMP"
expect "El Empleado sí consulta la biblioteca" 200

echo "== Baja (Q17)"
req DELETE "/equipment/DeleteEquipment/$EQ" "$ADMIN"
expect "No se da de baja con asignación activa" 409
req PATCH "/assignment/ReleaseEquipment/$ASG" "$ADMIN" '{}'
expect "Liberar asignación" 200
req GET "/equipment/GetEquipmentById/$EQ" "$ADMIN"
[[ "$(json "d['status']")" == "AVAILABLE" ]] && pass "Liberar deja el equipo Disponible" || fail "Estado tras liberar" "$BODY"
req DELETE "/equipment/DeleteEquipment/$EQ" "$ADMIN"
expect "Dar de baja (Administrador)" 204
req PATCH "/equipment/PatchEquipmentStatus/$EQ" "$ADMIN" '{"status":"AVAILABLE"}'
expect "Un equipo dado de baja no vuelve a servicio" 400

echo
echo "Resultado: $OK OK, $FAIL fallas.  Datos de prueba: equipo QA-S6-$RUN, usuario $EMP_EMAIL"
[[ $FAIL -eq 0 ]]
