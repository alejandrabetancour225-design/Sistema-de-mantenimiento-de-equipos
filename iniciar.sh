#!/usr/bin/env bash
# =====================================================================================
#  Arranca todo el sistema con un solo comando (Linux / macOS).
#
#    ./iniciar.sh              Todo en contenedores -> http://localhost:8080
#    ./iniciar.sh --dev        Modo desarrollo: IA en contenedores; API (dotnet watch) y
#                              frontend (Vite) con recarga en caliente -> http://localhost:5173
#    ./iniciar.sh --db-local   Usa un Postgres local en vez de Neon (se combina con --dev)
#    ./iniciar.sh --demo       Carga datos de demostración (no duplica si ya existen)
#    ./iniciar.sh --detener    Detiene todos los contenedores
#
#  Requisitos: Docker Desktop o Podman. Para --dev además .NET 10 SDK y Node 22.
#  En Windows usa iniciar.cmd (doble clic) o iniciar.ps1.
# =====================================================================================
set -euo pipefail
cd "$(dirname "$0")"

DEV=false; DB_LOCAL=false; DEMO=false; STOP=false
for arg in "$@"; do
  case "$arg" in
    --dev) DEV=true ;;
    --db-local) DB_LOCAL=true ;;
    --demo) DEMO=true ;;
    --detener|--stop) STOP=true ;;
    -h|--help) sed -n '2,15p' "$0"; exit 0 ;;
    *) echo "Opción desconocida: $arg (usa --help)"; exit 1 ;;
  esac
done

info()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn()  { printf '\033[1;33m[!]\033[0m %s\n' "$*"; }
fatal() { printf '\033[1;31m[x]\033[0m %s\n' "$*" >&2; exit 1; }

# ---------- Motor de contenedores ----------
if docker compose version >/dev/null 2>&1; then COMPOSE=(docker compose)
elif podman compose version >/dev/null 2>&1; then COMPOSE=(podman compose)
elif command -v docker-compose >/dev/null 2>&1; then COMPOSE=(docker-compose)
elif command -v podman-compose >/dev/null 2>&1; then COMPOSE=(podman-compose)
else fatal "No encontré Docker ni Podman. Instala Docker Desktop: https://docs.docker.com/get-docker/"
fi

if $STOP; then
  info "Deteniendo contenedores"
  "${COMPOSE[@]}" --profile db-local down
  exit 0
fi

# ---------- .env ----------
if [[ ! -f .env ]]; then
  cp .env.example .env
  fatal "Creé el archivo .env a partir de .env.example. Complétalo (DB_PASSWORD, JWT_KEY, ENCRYPTION_KEY: pídelos al líder del equipo) y vuelve a ejecutar."
fi
set -a
# shellcheck disable=SC1091
. ./.env
set +a

missing=()
for var in JWT_KEY ENCRYPTION_KEY; do [[ -z "${!var:-}" ]] && missing+=("$var"); done
if ! $DB_LOCAL; then
  for var in DB_HOST DB_NAME DB_USER DB_PASSWORD; do [[ -z "${!var:-}" ]] && missing+=("$var"); done
fi
((${#missing[@]})) && fatal "Faltan valores en .env: ${missing[*]}. Pídelos al líder del equipo."

OLLAMA_MODEL="${OLLAMA_MODEL:-llama3.1:8b}"
APP_PORT="${APP_PORT:-8080}"
PROFILE_ARGS=()
if $DB_LOCAL; then
  PROFILE_ARGS=(--profile db-local)
  export DB_HOST=postgres DB_PORT=5432 DB_NAME=database DB_USER=postgres DB_SSLMODE=Disable
  export DB_PASSWORD="${POSTGRES_PASSWORD:-postgres-local}"
  $DEV && export DB_HOST=localhost
fi

# ---------- Primer arranque ----------
if [[ ! -f .iniciado ]] && ! $DEMO; then
  read -r -p "Primer arranque: ¿cargar datos de demostración? [s/N] " answer || true
  case "${answer:-}" in [sS]*) DEMO=true ;; esac
fi
$DEMO && export SEED_DEMO=true

# ---------- Memoria ----------
ram_gb=0
if [[ -r /proc/meminfo ]]; then ram_gb=$(awk '/MemTotal/ {printf "%d", $2/1024/1024}' /proc/meminfo)
elif command -v sysctl >/dev/null 2>&1; then ram_gb=$(( $(sysctl -n hw.memsize 2>/dev/null || echo 0) / 1024 / 1024 / 1024 ))
fi
if (( ram_gb > 0 && ram_gb < 8 )); then
  warn "Este equipo tiene ${ram_gb} GB de RAM; el mínimo recomendado es 8 GB (16 GB ideal). La IA puede ir lenta."
fi

wait_for() { # wait_for URL segundos descripción
  local url=$1 seconds=$2 what=$3
  for ((i = 0; i < seconds; i++)); do
    curl -fsS -o /dev/null "$url" 2>/dev/null && return 0
    sleep 1
  done
  return 1
}

open_browser() {
  if command -v xdg-open >/dev/null 2>&1; then xdg-open "$1" >/dev/null 2>&1 || true
  elif command -v open >/dev/null 2>&1; then open "$1" || true
  fi
}

# ---------- IA: Ollama y el modelo ----------
info "Levantando Ollama"
"${COMPOSE[@]}" up -d ollama
wait_for "http://localhost:11434/api/tags" 60 "Ollama" || fatal "Ollama no respondió. Revisa: ${COMPOSE[*]} logs ollama"
if ! curl -fsS http://localhost:11434/api/tags | grep -q "\"name\":\"${OLLAMA_MODEL}\""; then
  info "Descargando el modelo ${OLLAMA_MODEL} (solo la primera vez; puede tardar varios minutos)"
  "${COMPOSE[@]}" exec ollama ollama pull "$OLLAMA_MODEL"
fi

if ! $DEV; then
  # ---------- Modo normal: todo en contenedores ----------
  info "Construyendo y levantando el sistema (la primera vez tarda unos minutos)"
  "${COMPOSE[@]}" ${PROFILE_ARGS[@]+"${PROFILE_ARGS[@]}"} up -d --build --force-recreate
  info "Esperando a la API (aplica migraciones al arrancar)"
  if ! wait_for "http://localhost:5255/api/health" 300 "API"; then
    "${COMPOSE[@]}" logs --tail 40 backend || true
    fatal "La API no arrancó. Arriba están sus últimos mensajes."
  fi
  touch .iniciado
  url="http://localhost:${APP_PORT}"
  info "Listo: $url"
  echo "    Detener:  ./iniciar.sh --detener      Logs:  ${COMPOSE[*]} logs -f backend"
  $DEMO && echo "    Usuarios demo: empleado1@demo.local, tecnico1@demo.local, gerente1@demo.local (contraseña DemoSprint6x2026)"
  open_browser "$url"
  exit 0
fi

# ---------- Modo desarrollo ----------
command -v dotnet >/dev/null 2>&1 || fatal "El modo --dev necesita el SDK de .NET 10: https://dotnet.microsoft.com/download"
command -v npm >/dev/null 2>&1 || fatal "El modo --dev necesita Node.js 22: https://nodejs.org"

info "Levantando servicios de apoyo (IA$($DB_LOCAL && echo ' y Postgres local'))"
"${COMPOSE[@]}" stop frontend backend >/dev/null 2>&1 || true
services=(python-service)
$DB_LOCAL && services+=(postgres)
"${COMPOSE[@]}" ${PROFILE_ARGS[@]+"${PROFILE_ARGS[@]}"} up -d --build --force-recreate "${services[@]}"

export ASPNETCORE_ENVIRONMENT=Development
export Database__Host="$DB_HOST" Database__Port="${DB_PORT:-5432}" Database__Name="$DB_NAME" \
  Database__User="$DB_USER" Database__Password="$DB_PASSWORD" Database__SslMode="${DB_SSLMODE:-Require}" \
  Database__MigrateOnStartup=true Jwt__Key="$JWT_KEY" Encryption__Key="$ENCRYPTION_KEY" \
  Bootstrap__AdminEmail="${BOOTSTRAP_ADMIN_EMAIL:-}" Bootstrap__AdminPassword="${BOOTSTRAP_ADMIN_PASSWORD:-}" \
  Bootstrap__SuperAdminEmail="${BOOTSTRAP_SUPERADMIN_EMAIL:-}" Bootstrap__SuperAdminPassword="${BOOTSTRAP_SUPERADMIN_PASSWORD:-}" \
  Seed__Demo="${SEED_DEMO:-false}" Email__Host="${SMTP_HOST:-}" Email__Port="${SMTP_PORT:-587}" \
  Email__User="${SMTP_USER:-}" Email__Password="${SMTP_PASSWORD:-}" Ai__BaseUrl=http://localhost:8000 \
  Ai__TimeoutSeconds="${AI_TIMEOUT_SECONDS:-190}" VITE_API_URL=/api DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true

[[ -d Frontend/node_modules ]] || { info "Instalando dependencias del frontend"; npm --prefix Frontend ci --no-audit --no-fund; }

pids=()
cleanup() {
  trap - INT TERM EXIT
  info "Deteniendo API y frontend (los contenedores siguen; ./iniciar.sh --detener para apagarlos)"
  for pid in "${pids[@]}"; do kill "$pid" 2>/dev/null || true; done
  wait 2>/dev/null || true
}
trap cleanup INT TERM EXIT

info "API con recarga en caliente (dotnet watch) en http://localhost:5255"
dotnet watch --project src/Backend.API run --no-launch-profile --urls http://localhost:5255 &
pids+=($!)
info "Frontend con recarga en caliente (Vite) en http://localhost:5173"
npm --prefix Frontend run dev -- --strictPort &
pids+=($!)

if wait_for "http://localhost:5255/api/health" 300 "API"; then
  touch .iniciado
  info "Listo: http://localhost:5173   (Ctrl+C para detener)"
  open_browser "http://localhost:5173"
else
  warn "La API aún no responde; revisa los mensajes de dotnet arriba."
fi
wait
