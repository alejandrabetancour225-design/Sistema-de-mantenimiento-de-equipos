#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Verificación manual de multiempresa (Sprint 6, fase 3) contra la API en marcha.
# Crea dos organizaciones de prueba (QA-S6 A y B) con su administrador.
#
# Uso: SUPERADMIN_EMAIL=... SUPERADMIN_PASSWORD=... ./verificacion/fase3.sh
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
token_of() { python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))"; }

SA=$(login_body "$SUPERADMIN_EMAIL" "$SUPERADMIN_PASSWORD" | token_of)
[[ -z "$SA" ]] && { echo "No se pudo iniciar sesión como SuperAdmin."; exit 1; }

echo "== Registro público eliminado"
CODE=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$API/auth/register" -H "Content-Type: application/json" -d '{"fullName":"x","email":"x@x.com","password":"Abcdefgh123"}')
[[ "$CODE" == "404" || "$CODE" == "405" ]] && pass "POST /auth/register ya no existe ($CODE)" || fail "Registro público" "$CODE"

echo "== Organizaciones (SuperAdmin)"
TEMP="Temporal${RUN}x"
for X in A B; do
  req POST /organizations "$SA" "{\"name\":\"QA-S6 $X $RUN\",\"plan\":\"BASICO\",\"administrator\":{\"fullName\":\"Admin $X\",\"email\":\"admin.$X.$RUN@example.com\",\"role\":\"Administrador\",\"temporaryPassword\":\"$TEMP\"}}"
  expect "Crear organización $X con su administrador" 200
done
req GET /equipment/GetEquipmentList "$SA"
expect "El SuperAdmin no entra a los datos de las empresas" 403

echo "== Contraseña temporal"
BODY=$(login_body "admin.A.$RUN@example.com" "$TEMP")
[[ "$(json "d.get('mustChangePassword')")" == "True" ]] && pass "El login avisa que la contraseña es temporal" || fail "mustChangePassword" "$BODY"
[[ "$(json "d.get('organizationName')")" == "QA-S6 A $RUN" ]] && pass "El login informa la organización" || fail "organizationName" "$BODY"
TA=$(token_of <<<"$BODY")
req GET /equipment/GetEquipmentList "$TA"
expect "Con contraseña temporal la API no deja trabajar" 403
req POST /auth/change-password "$TA" "{\"currentPassword\":\"$TEMP\",\"newPassword\":\"Definitiva${RUN}A\"}"
expect "Cambiar la contraseña temporal" 200
TA=$(json "d['token']")
TB=$(login_body "admin.B.$RUN@example.com" "$TEMP" | token_of)
req POST /auth/change-password "$TB" "{\"currentPassword\":\"$TEMP\",\"newPassword\":\"Definitiva${RUN}B\"}"
TB=$(json "d['token']")

echo "== Aislamiento de datos"
EQ='{"internalCode":"QA-S6-MT","serialNumber":"SN-QA-S6-MT","type":"Laptop","brand":"Dell","model":"X","location":"Bodega"}'
req POST /equipment/PostNewEquipment "$TA" "$EQ"; expect "Empresa A registra QA-S6-MT" 200
EQ_A=$(json "d['id']")
req POST /equipment/PostNewEquipment "$TB" "$EQ"; expect "Empresa B puede usar el mismo código" 200
req GET /equipment/GetEquipmentList "$TB"
[[ "$CODE" == "200" && "$(json "len(d)")" == "1" ]] && pass "Empresa B solo ve su equipo" || fail "Lista de B" "$(json "len(d)") equipos"
req GET "/equipment/GetEquipmentById/$EQ_A" "$TB"; expect "Empresa B no abre el equipo de A" 404
req GET /users "$TB"
[[ "$CODE" == "200" && "$(json "len(d)")" == "1" ]] && pass "Empresa B solo ve sus usuarios" || fail "Usuarios de B" "$(json "len(d)")"

echo "== Usuarios creados por el Administrador"
req POST /users "$TA" "{\"fullName\":\"Empleado A\",\"email\":\"emp.A.$RUN@example.com\",\"role\":\"Empleado\",\"temporaryPassword\":\"$TEMP\"}"
expect "El Administrador crea un Empleado en su empresa" 200
req POST /users "$TA" "{\"fullName\":\"Intruso\",\"email\":\"sa.$RUN@example.com\",\"role\":\"SuperAdmin\",\"temporaryPassword\":\"$TEMP\"}"
expect "No puede crear un SuperAdmin" 400
req POST /users "$TB" "{\"fullName\":\"Repetido\",\"email\":\"emp.A.$RUN@example.com\",\"role\":\"Empleado\",\"temporaryPassword\":\"$TEMP\"}"
expect "El correo es único en toda la plataforma" 409

echo "== Organización desactivada"
ORG_B=$(curl -s "$API/organizations" -H "Authorization: Bearer $SA" | python3 -c "import sys,json; print([o['id'] for o in json.load(sys.stdin) if o['name']=='QA-S6 B $RUN'][0])")
req PUT "/organizations/$ORG_B" "$SA" "{\"name\":\"QA-S6 B $RUN\",\"plan\":\"BASICO\",\"active\":false}"
expect "Desactivar la organización B" 200
req GET /equipment/GetEquipmentList "$TB"; expect "El token de B deja de servir" 401
CODE=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$API/auth/login" -H "Content-Type: application/json" -d "{\"email\":\"admin.B.$RUN@example.com\",\"password\":\"Definitiva${RUN}B\"}")
[[ "$CODE" == "401" ]] && pass "B no puede iniciar sesión (401)" || fail "Login de B" "$CODE"

echo
echo "Resultado: $OK OK, $FAIL fallas.  Organizaciones de prueba: 'QA-S6 A $RUN' y 'QA-S6 B $RUN'"
[[ $FAIL -eq 0 ]]
