#!/usr/bin/env bash
# ---------------------------------------------------------------
# Seed: puebla el sistema con datos de prueba para el dashboard.
# Uso:  ./seed.sh
# ---------------------------------------------------------------

set -euo pipefail

API_URL="${API_URL:-http://localhost:5255}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@test.com}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-AdminSegura123}"

GREEN='\033[0;32m'; RED='\033[0;31m'; YELLOW='\033[1;33m'; NC='\033[0m'

log()  { echo -e "${GREEN}✓${NC} $1"; }
warn() { echo -e "${YELLOW}→${NC} $1"; }
fail() { echo -e "${RED}✗${NC} $1"; exit 1; }

# ---------------------------------------------------------------
# 1. Login como admin
# ---------------------------------------------------------------
warn "Login como $ADMIN_EMAIL"
LOGIN=$(curl -s -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}")

TOKEN=$(echo "$LOGIN" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
ADMIN_ID=$(echo "$LOGIN" | grep -o '"userId":"[^"]*' | cut -d'"' -f4)

[[ -z "$TOKEN" ]] && fail "No se pudo autenticar. Revisa las credenciales."

AUTH=(-H "Authorization: Bearer $TOKEN" -H "X-CSRF: 1" -H "Content-Type: application/json")
log "Autenticado. Admin ID: $ADMIN_ID"

# ---------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------

# Crea un usuario y devuelve su ID
create_user() {
  local email="$1" name="$2" password="$3"
  local body
  body=$(curl -s -X POST "$API_URL/api/auth/register" \
    -H "Content-Type: application/json" \
    -d "{\"fullName\":\"$name\",\"email\":\"$email\",\"password\":\"$password\",\"phone\":\"3000000000\"}")
  echo "$body" | grep -o '"userId":"[^"]*' | cut -d'"' -f4
}

# Cambia el rol de un usuario
set_role() {
  local uid="$1" role="$2"
  curl -s -X PUT "$API_URL/api/users/$uid/role" \
    "${AUTH[@]}" -d "{\"role\":\"$role\"}" > /dev/null
}

# Crea un equipo y devuelve su ID
create_equipment() {
  local code="$1" status="${2:-AVAILABLE}"
  local body
  body=$(curl -s -X POST "$API_URL/api/equipment/PostNewEquipment" \
    "${AUTH[@]}" -d "{
      \"internalCode\": \"$code\",
      \"serialNumber\": \"SN-$code\",
      \"type\": \"Laptop\",
      \"brand\": \"Dell\",
      \"model\": \"Latitude 5420\",
      \"location\": \"Oficina 201\"
    }")
  local id
  id=$(echo "$body" | grep -o '"id":"[^"]*' | head -1 | cut -d'"' -f4)
  echo "$id"
}

# Cambia estado de un equipo
set_equipment_status() {
  local eq_id="$1" status="$2"
  curl -s -X PATCH "$API_URL/api/equipment/PatchEquipmentStatus/$eq_id" \
    "${AUTH[@]}" -d "{\"status\":\"$status\"}" > /dev/null
}

# Crea una incidencia (con fecha controlada)
create_incident() {
  local eq_id="$1" desc="$2" reported_at="$3"
  local body
  body=$(curl -s -X POST "$API_URL/api/incident/PostNewIncident" \
    "${AUTH[@]}" -d "{
      \"equipmentId\": \"$eq_id\",
      \"description\": \"$desc\",
      \"reportedAt\": \"$reported_at\"
    }")
  echo "$body" | grep -o '"id":"[^"]*' | head -1 | cut -d'"' -f4
}

# Crea un mantenimiento
create_maintenance() {
  local eq_id="$1" tech_id="$2" type="$3" next_date="$4"
  local body
  body=$(curl -s -X POST "$API_URL/api/maintenance/PostNewMaintenance" \
    "${AUTH[@]}" -d "{
      \"equipmentId\": \"$eq_id\",
      \"technicianId\": \"$tech_id\",
      \"type\": \"$type\",
      \"reportedProblem\": \"Mantenimiento $type programado\",
      \"laborCost\": 50.00,
      \"otherCosts\": 10.00,
      \"nextMaintenanceDate\": \"$next_date\",
      \"observations\": \"Seed automático\"
    }")
  echo "$body" | grep -o '"id":"[^"]*' | head -1 | cut -d'"' -f4
}

# ---------------------------------------------------------------
# 2. Crear usuarios
# ---------------------------------------------------------------
warn "Creando usuarios"
EMP1_ID=$(create_user "emp1@test.com" "Empleado Uno" "Empleado1234")
EMP2_ID=$(create_user "emp2@test.com" "Empleado Dos" "Empleado1234")
TEC1_ID=$(create_user "tec1@test.com" "Técnico Uno" "Tecnico1234")
TEC2_ID=$(create_user "tec2@test.com" "Técnico Dos" "Tecnico1234")

set_role "$EMP1_ID" "Empleado"
set_role "$EMP2_ID" "Empleado"
set_role "$TEC1_ID" "Técnico"
set_role "$TEC2_ID" "Técnico"

log "Usuarios creados y roles asignados"
echo "  Empleado 1: $EMP1_ID"
echo "  Empleado 2: $EMP2_ID"
echo "  Técnico 1:  $TEC1_ID"
echo "  Técnico 2:  $TEC2_ID"

# ---------------------------------------------------------------
# 3. Crear equipos
# ---------------------------------------------------------------
warn "Creando equipos"

EQ1=$(create_equipment "EQ-SEED-001")
EQ2=$(create_equipment "EQ-SEED-002")
EQ3=$(create_equipment "EQ-SEED-003")
EQ4=$(create_equipment "EQ-SEED-004")
EQ5=$(create_equipment "EQ-SEED-005")
EQ6=$(create_equipment "EQ-SEED-006")
EQ7=$(create_equipment "EQ-SEED-007")
EQ8=$(create_equipment "EQ-SEED-008")

log "8 equipos creados (todos en AVAILABLE)"

# ---------------------------------------------------------------
# 4. Asignar 3 equipos a empleados
# ---------------------------------------------------------------
warn "Asignando equipos"

assign() {
  local eq_id="$1" uid="$2"
  curl -s -X POST "$API_URL/api/assignment/AssignEquipment" \
    "${AUTH[@]}" -d "{
      \"equipmentId\": \"$eq_id\",
      \"userId\": \"$uid\",
      \"observations\": \"Asignación de prueba\"
    }" > /dev/null
}

assign "$EQ1" "$EMP1_ID"
assign "$EQ2" "$EMP1_ID"
assign "$EQ3" "$EMP2_ID"

log "3 asignaciones activas (EQ-SEED-001, 002, 003 → IN_USE automáticamente)"

# ---------------------------------------------------------------
# 5. Estados variados
# ---------------------------------------------------------------
warn "Variando estados"

# EQ-SEED-004 y 005 → OUT_OF_SERVICE
set_equipment_status "$EQ4" "OUT_OF_SERVICE"
set_equipment_status "$EQ5" "OUT_OF_SERVICE"

# EQ-SEED-006 → DECOMMISSIONED (vía soft-delete)
curl -s -X DELETE "$API_URL/api/equipment/DeleteEquipment/$EQ6" "${AUTH[@]}" > /dev/null

log "EQ-SEED-004, 005 → OUT_OF_SERVICE"
log "EQ-SEED-006 → DECOMMISSIONED"

# ---------------------------------------------------------------
# 6. Crear incidencias
# ---------------------------------------------------------------
warn "Creando incidencias"

# Una reciente (no urgente)
create_incident "$EQ7" "Pantalla parpadea intermitentemente" "$(date -u +"%Y-%m-%dT%H:%M:%SZ")" > /dev/null

# Una con 5 días (urgente)
create_incident "$EQ8" "El equipo se sobrecalienta" \
  "$(date -u -d '5 days ago' +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -v-5d +"%Y-%m-%dT%H:%M:%SZ")" > /dev/null

log "2 incidencias creadas (una con +3 días → urgente)"

# ---------------------------------------------------------------
# 7. Crear mantenimientos
# ---------------------------------------------------------------
warn "Creando mantenimientos"

# Preventivo próximo (10 días)
create_maintenance "$EQ1" "$TEC1_ID" "PREVENTIVE" \
  "$(date -u -d '+10 days' +"%Y-%m-%d" 2>/dev/null || date -u -v+10d +"%Y-%m-%d")" > /dev/null

# Preventivo vencido (hace 3 días)
create_maintenance "$EQ2" "$TEC2_ID" "PREVENTIVE" \
  "$(date -u -d '-3 days' +"%Y-%m-%d" 2>/dev/null || date -u -v-3d +"%Y-%m-%d")" > /dev/null

# Preventivo lejano (25 días)
create_maintenance "$EQ4" "$TEC1_ID" "PREVENTIVE" \
  "$(date -u -d '+25 days' +"%Y-%m-%d" 2>/dev/null || date -u -v+25d +"%Y-%m-%d")" > /dev/null

log "3 mantenimientos programados (uno vencido)"

# ---------------------------------------------------------------
# 8. Crear repuestos
# ---------------------------------------------------------------
warn "Creando repuestos"

curl -s -X POST "$API_URL/api/sparepart/PostNewSparePart" \
  "${AUTH[@]}" -d '{
    "name": "Memoria RAM DDR4 16GB",
    "description": "Kingston KVR32S22S8/16",
    "unitCost": 45.50
  }' > /dev/null

curl -s -X POST "$API_URL/api/sparepart/PostNewSparePart" \
  "${AUTH[@]}" -d '{
    "name": "Disco SSD 512GB",
    "description": "Samsung 870 EVO",
    "unitCost": 89.99
  }' > /dev/null

log "2 repuestos creados"

# ---------------------------------------------------------------
# 9. Resumen
# ---------------------------------------------------------------
echo
echo -e "${GREEN}═══════════════════════════════════════════${NC}"
echo -e "${GREEN}  Seed completado${NC}"
echo -e "${GREEN}═══════════════════════════════════════════${NC}"
echo "  Admin:         $ADMIN_EMAIL / $ADMIN_PASSWORD"
echo "  Empleados:     emp1@test.com, emp2@test.com (pass: Empleado1234)"
echo "  Técnicos:      tec1@test.com, tec2@test.com (pass: Tecnico1234)"
echo "  Equipos:       8 (3 IN_USE, 2 OUT_OF_SERVICE, 1 DECOMMISSIONED, 2 AVAILABLE)"
echo "  Asignaciones:  3 activas"
echo "  Incidencias:   2 abiertas (1 urgente)"
echo "  Mantenimientos: 3 (1 vencido, 1 próximo, 1 lejano)"
echo "  Repuestos:     2"
echo
echo "  Ahora abre http://localhost:5173/dashboard"
