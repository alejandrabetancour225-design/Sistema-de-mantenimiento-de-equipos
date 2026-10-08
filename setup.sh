#!/bin/sh

PROJECT="src/Backend.API"

dotnet user-secrets set "Jwt:Key" "ZJfV/E5RCxSYXDh8YE54fus6PRr20bYKj7NxUV6EvRP9WTeDcdPKVeoHyoRequcMxmj7Q/EM6h/sxHIY3RBlhQ==" --project "$PROJECT"
dotnet user-secrets set "Encryption:Key" "6rFo7LNX5I95pY4AiEJt7PVsaazsJOcSA1LMPyPLi+E=" --project "$PROJECT"
dotnet user-secrets set "Database:Password" "npg_iFWwZA9cEbm6" --project "$PROJECT"

echo ""
echo "Secretos configurados. Ya puedes ejecutar:"
echo "dotnet run --project $PROJECT"
