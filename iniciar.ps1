# =====================================================================================
#  Arranca todo el sistema con un solo comando (Windows / PowerShell).
#
#    .\iniciar.ps1              Todo en contenedores -> http://localhost:8080
#    .\iniciar.ps1 -Dev         Modo desarrollo: IA en contenedores; API (dotnet watch) y
#                               frontend (Vite) con recarga en caliente -> http://localhost:5173
#    .\iniciar.ps1 -DbLocal     Usa un Postgres local en vez de Neon (se combina con -Dev)
#    .\iniciar.ps1 -Demo        Carga datos de demostración (no duplica si ya existen)
#    .\iniciar.ps1 -Detener     Detiene todos los contenedores
#
#  También se puede abrir con doble clic en iniciar.cmd.
#  Requisitos: Docker Desktop (o Podman). Para -Dev además .NET 10 SDK y Node 22.
# =====================================================================================
param(
    [switch]$Dev,
    [switch]$DbLocal,
    [switch]$Demo,
    [switch]$Detener
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

function Info($msg)  { Write-Host "==> $msg" -ForegroundColor Cyan }
function Warn($msg)  { Write-Host "[!] $msg" -ForegroundColor Yellow }
function Fatal($msg) { Write-Host "[x] $msg" -ForegroundColor Red; Read-Host "Presiona Enter para salir" | Out-Null; exit 1 }

function Test-Command($cmd) { $null -ne (Get-Command $cmd -ErrorAction SilentlyContinue) }

# ---------- Motor de contenedores ----------
$Compose = $null
foreach ($candidate in @(@("docker", "compose"), @("podman", "compose"))) {
    if (Test-Command $candidate[0]) {
        & $candidate[0] $candidate[1] version *> $null
        if ($LASTEXITCODE -eq 0) { $Compose = $candidate; break }
    }
}
if (-not $Compose) {
    Fatal "No encontré Docker ni Podman. Instala Docker Desktop: https://docs.docker.com/desktop/setup/install/windows-install/ (y ábrelo antes de ejecutar este script)."
}
function Invoke-Compose { & $Compose[0] $Compose[1] @args; if ($LASTEXITCODE -ne 0) { throw "Falló: $($Compose -join ' ') $args" } }

if ($Detener) {
    Info "Deteniendo contenedores"
    Invoke-Compose --profile db-local down
    exit 0
}

# ---------- .env ----------
if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Fatal "Creé el archivo .env a partir de .env.example. Complétalo (DB_PASSWORD, JWT_KEY, ENCRYPTION_KEY: pídelos al líder del equipo) y vuelve a ejecutar."
}
$EnvVars = @{}
foreach ($line in Get-Content ".env") {
    if ($line -match '^\s*([A-Z_][A-Z0-9_]*)=(.*)$') {
        $EnvVars[$Matches[1]] = $Matches[2].Trim()
        [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2].Trim(), "Process")
    }
}
function Get-Setting($name, $default = "") {
    $value = [Environment]::GetEnvironmentVariable($name, "Process")
    if ([string]::IsNullOrWhiteSpace($value)) { return $default } else { return $value }
}
function Set-Setting($name, $value) { [Environment]::SetEnvironmentVariable($name, $value, "Process") }

$required = @("JWT_KEY", "ENCRYPTION_KEY")
if (-not $DbLocal) { $required += @("DB_HOST", "DB_NAME", "DB_USER", "DB_PASSWORD") }
$missing = $required | Where-Object { [string]::IsNullOrWhiteSpace((Get-Setting $_)) }
if ($missing) { Fatal "Faltan valores en .env: $($missing -join ', '). Pídelos al líder del equipo." }

$Model = Get-Setting "OLLAMA_MODEL" "llama3.1:8b"
$AppPort = Get-Setting "APP_PORT" "8080"
$ProfileArgs = @()
if ($DbLocal) {
    $ProfileArgs = @("--profile", "db-local")
    Set-Setting "DB_HOST" ($(if ($Dev) { "localhost" } else { "postgres" }))
    Set-Setting "DB_PORT" "5432"; Set-Setting "DB_NAME" "database"; Set-Setting "DB_USER" "postgres"
    Set-Setting "DB_SSLMODE" "Disable"; Set-Setting "DB_PASSWORD" (Get-Setting "POSTGRES_PASSWORD" "postgres-local")
}

# ---------- Primer arranque ----------
if (-not (Test-Path ".iniciado") -and -not $Demo) {
    $answer = Read-Host "Primer arranque: ¿cargar datos de demostración? [s/N]"
    if ($answer -match '^[sS]') { $Demo = $true }
}
if ($Demo) { Set-Setting "SEED_DEMO" "true" }

# ---------- Memoria ----------
try {
    $ramGb = [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB)
    if ($ramGb -lt 8) { Warn "Este equipo tiene $ramGb GB de RAM; el mínimo recomendado es 8 GB (16 GB ideal). La IA puede ir lenta." }
} catch { }

function Wait-Url($url, $seconds) {
    for ($i = 0; $i -lt $seconds; $i++) {
        try { Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3 | Out-Null; return $true } catch { Start-Sleep -Seconds 1 }
    }
    return $false
}

# ---------- IA: Ollama y el modelo ----------
Info "Levantando Ollama"
Invoke-Compose up -d ollama
if (-not (Wait-Url "http://localhost:11434/api/tags" 60)) { Fatal "Ollama no respondió. Revisa: $($Compose -join ' ') logs ollama" }
$tags = (Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -UseBasicParsing).Content
if ($tags -notmatch [regex]::Escape("`"name`":`"$Model`"")) {
    Info "Descargando el modelo $Model (solo la primera vez; puede tardar varios minutos)"
    Invoke-Compose exec ollama ollama pull $Model
}

if (-not $Dev) {
    # ---------- Modo normal: todo en contenedores ----------
    Info "Construyendo y levantando el sistema (la primera vez tarda unos minutos)"
    Invoke-Compose @ProfileArgs up -d --build --force-recreate
    Info "Esperando a la API (aplica migraciones al arrancar)"
    if (-not (Wait-Url "http://localhost:5255/api/health" 300)) {
        & $Compose[0] $Compose[1] logs --tail 40 backend
        Fatal "La API no arrancó. Arriba están sus últimos mensajes."
    }
    New-Item -ItemType File -Path ".iniciado" -Force | Out-Null
    $url = "http://localhost:$AppPort"
    Info "Listo: $url"
    Write-Host "    Detener:  .\iniciar.ps1 -Detener      Logs:  $($Compose -join ' ') logs -f backend"
    if ($Demo) { Write-Host "    Usuarios demo: empleado1@demo.local, tecnico1@demo.local, gerente1@demo.local (contraseña DemoSprint6x2026)" }
    Start-Process $url
    exit 0
}

# ---------- Modo desarrollo ----------
if (-not (Test-Command "dotnet")) { Fatal "El modo -Dev necesita el SDK de .NET 10: https://dotnet.microsoft.com/download" }
if (-not (Test-Command "npm")) { Fatal "El modo -Dev necesita Node.js 22: https://nodejs.org" }

Info "Levantando servicios de apoyo"
& $Compose[0] $Compose[1] stop frontend backend *> $null
$services = @("python-service")
if ($DbLocal) { $services += "postgres" }
Invoke-Compose @ProfileArgs up -d --build --force-recreate @services

$map = @{
    "ASPNETCORE_ENVIRONMENT" = "Development"
    "Database__Host" = (Get-Setting "DB_HOST"); "Database__Port" = (Get-Setting "DB_PORT" "5432")
    "Database__Name" = (Get-Setting "DB_NAME"); "Database__User" = (Get-Setting "DB_USER")
    "Database__Password" = (Get-Setting "DB_PASSWORD"); "Database__SslMode" = (Get-Setting "DB_SSLMODE" "Require")
    "Database__MigrateOnStartup" = "true"
    "Jwt__Key" = (Get-Setting "JWT_KEY"); "Encryption__Key" = (Get-Setting "ENCRYPTION_KEY")
    "Bootstrap__AdminEmail" = (Get-Setting "BOOTSTRAP_ADMIN_EMAIL"); "Bootstrap__AdminPassword" = (Get-Setting "BOOTSTRAP_ADMIN_PASSWORD")
    "Bootstrap__SuperAdminEmail" = (Get-Setting "BOOTSTRAP_SUPERADMIN_EMAIL"); "Bootstrap__SuperAdminPassword" = (Get-Setting "BOOTSTRAP_SUPERADMIN_PASSWORD")
    "Seed__Demo" = (Get-Setting "SEED_DEMO" "false")
    "Email__Host" = (Get-Setting "SMTP_HOST"); "Email__Port" = (Get-Setting "SMTP_PORT" "587")
    "Email__User" = (Get-Setting "SMTP_USER"); "Email__Password" = (Get-Setting "SMTP_PASSWORD")
    "Ai__BaseUrl" = "http://localhost:8000"; "Ai__TimeoutSeconds" = (Get-Setting "AI_TIMEOUT_SECONDS" "190")
    "VITE_API_URL" = "/api"; "DOTNET_WATCH_RESTART_ON_RUDE_EDIT" = "true"
}
foreach ($key in $map.Keys) { Set-Setting $key $map[$key] }

if (-not (Test-Path "Frontend/node_modules")) {
    Info "Instalando dependencias del frontend"
    & npm --prefix Frontend ci --no-audit --no-fund
}

Info "API con recarga en caliente (dotnet watch) en http://localhost:5255"
$api = Start-Process -FilePath "dotnet" -ArgumentList @("watch", "--project", "src/Backend.API", "run", "--no-launch-profile", "--urls", "http://localhost:5255") -NoNewWindow -PassThru
Info "Frontend con recarga en caliente (Vite) en http://localhost:5173"
$npmCmd = if (Test-Command "npm.cmd") { "npm.cmd" } else { "npm" }
$web = Start-Process -FilePath $npmCmd -ArgumentList @("--prefix", "Frontend", "run", "dev", "--", "--strictPort") -NoNewWindow -PassThru

try {
    if (Wait-Url "http://localhost:5255/api/health" 300) {
        New-Item -ItemType File -Path ".iniciado" -Force | Out-Null
        Info "Listo: http://localhost:5173   (Ctrl+C para detener)"
        Start-Process "http://localhost:5173"
    } else {
        Warn "La API aún no responde; revisa los mensajes de dotnet arriba."
    }
    while (-not $api.HasExited -and -not $web.HasExited) { Start-Sleep -Seconds 1 }
}
finally {
    Info "Deteniendo API y frontend (los contenedores siguen; .\iniciar.ps1 -Detener para apagarlos)"
    foreach ($p in @($api, $web)) {
        if ($p -and -not $p.HasExited) { & taskkill /PID $p.Id /T /F *> $null }
    }
}
