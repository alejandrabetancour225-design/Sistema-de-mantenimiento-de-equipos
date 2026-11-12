#!/bin/sh
# Carga los secretos del archivo .env (NO versionado) en los user-secrets de .NET,
# para correr el backend con "dotnet run". El .env lo reparte el líder del equipo por un canal privado.
# Nota: en la fase 1 del Sprint 6 este script se reemplaza por iniciar.sh / iniciar.ps1.
set -eu
PROJECT="src/Backend.API"

if [ ! -f .env ]; then
  echo "Falta el archivo .env. Copia .env.example a .env y completa los valores (pídelos al líder del equipo)." >&2
  exit 1
fi

set -a
. ./.env
set +a

for var in JWT_KEY ENCRYPTION_KEY DB_PASSWORD; do
  eval "value=\${$var:-}"
  if [ -z "$value" ]; then
    echo "Falta $var en .env" >&2
    exit 1
  fi
done

dotnet user-secrets set "Jwt:Key" "$JWT_KEY" --project "$PROJECT" >/dev/null
dotnet user-secrets set "Encryption:Key" "$ENCRYPTION_KEY" --project "$PROJECT" >/dev/null
dotnet user-secrets set "Database:Password" "$DB_PASSWORD" --project "$PROJECT" >/dev/null

echo "Secretos configurados. Ya puedes ejecutar: dotnet run --project $PROJECT"
