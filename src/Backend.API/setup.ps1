$project = "src/Backend.API"

$secrets = @(
    @{ Key = "Jwt:Key"; Value = "ZJfV/E5RCxSYXDh8YE54fus6PRr20bYKj7NxUV6EvRP9WTeDcdPKVeoHyoRequcMxmj7Q/EM6h/sxHIY3RBlhQ==" },
    @{ Key = "Encryption:Key"; Value = "6rFo7LNX5I95pY4AiEJt7PVsaazsJOcSA1LMPyPLi+E=" },
    @{ Key = "Database:Password"; Value = "npg_iFWwZA9cEbm6" }
)

foreach ($s in $secrets) {
    dotnet user-secrets set $s.Key $s.Value --project $project
}

Write-Host ""
Write-Host "Secretos configurados. Ya puedes ejecutar: dotnet run --project $project"