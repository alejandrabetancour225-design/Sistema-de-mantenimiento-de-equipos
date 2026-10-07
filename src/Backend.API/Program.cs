using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// ---------- Límites de tamaño ----------
var maxRequestBodyBytes = builder.Configuration.GetValue<long>("Limits:MaxRequestBodyBytes", 1_048_576);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = maxRequestBodyBytes;
});

// ---------- Controladores, JSON y validación ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Los enums solo se aceptan por nombre ("OPEN"), nunca como número (99).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        options.JsonSerializerOptions.MaxDepth = 32;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Mismo formato { message } que el resto de la API, más el detalle por campo.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor inválido." : e.ErrorMessage)
                        .ToArray());

            var bodyIsMalformed = errors.Keys.Any(key => key.StartsWith('$') || key.Length == 0);
            var message = bodyIsMalformed
                ? "El cuerpo de la petición no es válido (revisa el formato y los valores permitidos)."
                : errors.Values.SelectMany(v => v).FirstOrDefault() ?? "Datos inválidos.";

            return new BadRequestObjectResult(new { message, errors });
        };
    });

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------- Base de datos ----------
var db = builder.Configuration.GetSection("Database");
if (string.IsNullOrWhiteSpace(db["Host"]) || string.IsNullOrWhiteSpace(db["Name"]) || string.IsNullOrWhiteSpace(db["User"]))
{
    throw new InvalidOperationException(
        "La sección Database (Host, Name, User, Password) no está configurada. Revisa appsettings.Development.json, user-secrets o variables de entorno (Database__Host, ...).");
}

var sslMode = db["SslMode"];
var connectionString = $"Host={db["Host"]};Port={db["Port"] ?? "5432"};Database={db["Name"]};Username={db["User"]};Password={db["Password"]}"
    + (string.IsNullOrEmpty(sslMode) ? "" : $";SslMode={sslMode}");

builder.Services.AddSingleton<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(connectionString)
        .AddInterceptors(services.GetRequiredService<AuditSaveChangesInterceptor>()));

// ---------- JWT ----------
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key no está configurado. En desarrollo: dotnet user-secrets set \"Jwt:Key\" <clave>; en producción: variable de entorno Jwt__Key.");
}

var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (jwtKeyBytes.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 bytes (256 bits).");
}

if (string.IsNullOrEmpty(builder.Configuration["Encryption:Key"]))
{
    throw new InvalidOperationException(
        "Encryption:Key no está configurado. En desarrollo: dotnet user-secrets set \"Encryption:Key\" <clave-base64-32-bytes>; en producción: variable de entorno Encryption__Key.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes)
        };

        options.Events = new JwtBearerEvents
        {
            // Acepta el token en la cabecera Authorization o en la cookie httpOnly.
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token)
                    && !context.Request.Headers.ContainsKey(HeaderNames.Authorization)
                    && context.Request.Cookies.TryGetValue(AuthCookie.Name, out var cookieToken)
                    && !string.IsNullOrEmpty(cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },

            // En cada petición se revisa el usuario en la BD: si se desactivó, cambió de rol,
            // cambió su contraseña o cerró sesión, el token deja de servir de inmediato.
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var subject = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(subject, out var userId))
                {
                    context.Fail("El token no contiene un identificador de usuario válido.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new
                    {
                        u.Active,
                        u.SecurityStamp,
                        RoleName = u.Role != null ? u.Role.Name : null
                    })
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

                if (user is null)
                {
                    context.Fail("El usuario del token ya no existe.");
                    return;
                }

                if (!user.Active)
                {
                    context.Fail("El usuario del token está inactivo.");
                    return;
                }

                var stampClaim = principal?.FindFirstValue(AppClaims.SecurityStamp);
                if (!Guid.TryParse(stampClaim, out var stamp) || stamp != user.SecurityStamp)
                {
                    context.Fail("La sesión ya no es válida. Inicia sesión de nuevo.");
                    return;
                }

                var roleClaim = principal?.FindFirstValue(ClaimTypes.Role);
                if (!string.Equals(roleClaim, user.RoleName, StringComparison.Ordinal))
                {
                    context.Fail("El rol del usuario cambió. Inicia sesión de nuevo.");
                }
            }
        };
    });
builder.Services.AddAuthorization();

// ---------- Límite de peticiones ----------
var globalPermitLimit = builder.Configuration.GetValue("RateLimiting:Global:PermitLimit", 300);
var loginPermitLimit = builder.Configuration.GetValue("RateLimiting:Login:PermitLimit", 10);
var registerPermitLimit = builder.Configuration.GetValue("RateLimiting:Register:PermitLimit", 5);

static string ClientIp(HttpContext httpContext) =>
    httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido";

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Demasiadas peticiones. Espera un momento e inténtalo de nuevo." },
            cancellationToken);
    };

    // Límite general por usuario autenticado (o por IP si no hay sesión).
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = AuditActor.GetUserId(httpContext)?.ToString() ?? ClientIp(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = globalPermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    // Inicio de sesión: pocos intentos por IP para frenar la fuerza bruta.
    options.AddPolicy(RateLimitPolicies.Login, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        ClientIp(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = loginPermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    // Registro: muy limitado por IP (frena la creación masiva de cuentas y la enumeración de correos).
    options.AddPolicy(RateLimitPolicies.Register, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        ClientIp(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = registerPermitLimit,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// ---------- CORS ----------
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (corsOrigins is { Length: > 0 })
        {
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Host == "localhost")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// ---------- Servicios ----------
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IEquipmentComponentService, EquipmentComponentService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<ISparePartService, SparePartService>();

var app = builder.Build();

// ---------- Tareas de arranque ----------
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Las migraciones ya no se aplican solas contra cualquier base: solo si se pide explícitamente
    // (true en appsettings.Development.json para la base local). En otros entornos usar
    // "dotnet ef database update" con un usuario con permisos de esquema.
    if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
    {
        app.Logger.LogInformation("Aplicando migraciones pendientes (Database:MigrateOnStartup=true)");
        dbContext.Database.Migrate();
    }

    if (app.Configuration.GetValue<bool>("Encryption:RotateOnStartup"))
    {
        var backupDirectory = app.Configuration["Encryption:RotationBackupDirectory"]
            ?? Path.Combine(AppContext.BaseDirectory, "encryption-backup");

        var report = await EncryptionRotationRunner.RunAsync(
            dbContext,
            scope.ServiceProvider.GetRequiredService<IEncryptionService>(),
            app.Logger,
            backupDirectory);

        app.Logger.LogInformation(
            "Rotacion de cifrado: revisados={Scanned}, rotados={Rotated}, ya vigentes={Current}, fallidos={Failed}, verificados={Verified}, respaldo={BackupPath}",
            report.Scanned, report.Rotated, report.AlreadyCurrent, report.Failed, report.Verified, report.BackupPath);

        if (report.Failed > 0 || report.Verified != report.Scanned)
        {
            throw new InvalidOperationException(
                "La rotacion de cifrado no se completo. Revisa el log y restaura el respaldo antes de volver a intentarlo.");
        }

        if (app.Configuration.GetValue("Encryption:RotateAndExit", true))
        {
            app.Logger.LogInformation(
                "Rotacion de cifrado finalizada. Reinicia la API con Encryption__RotateOnStartup=false para quitarla del modo rotacion.");
            return;
        }
    }

    await AdminBootstrapper.EnsureAdministratorAsync(scope.ServiceProvider, app.Configuration, app.Logger);
}

// ---------- Pipeline HTTP ----------
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Cabeceras de seguridad. La API solo devuelve JSON: no debe poder incrustarse ni ejecutar contenido.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers[HeaderNames.XContentTypeOptions] = "nosniff";
    headers[HeaderNames.XFrameOptions] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'; frame-ancestors 'none'";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers[HeaderNames.CacheControl] = "no-store";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();

app.UseCors("Frontend");

app.UseAuthentication();

// Después de la autenticación para poder limitar por usuario.
app.UseRateLimiter();

// Protección CSRF para la autenticación por cookie: las peticiones que modifican datos
// deben traer la cabecera X-CSRF (un formulario de otro sitio no puede agregarla).
app.Use(async (context, next) =>
{
    var request = context.Request;
    var isSafeMethod = HttpMethods.IsGet(request.Method)
        || HttpMethods.IsHead(request.Method)
        || HttpMethods.IsOptions(request.Method);
    var isAnonymousAuthEndpoint = request.Path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase)
        || request.Path.StartsWithSegments("/api/auth/register", StringComparison.OrdinalIgnoreCase);

    if (!isSafeMethod
        && !isAnonymousAuthEndpoint
        && !request.Headers.ContainsKey(HeaderNames.Authorization)
        && request.Cookies.ContainsKey(AuthCookie.Name)
        && !request.Headers.ContainsKey(AuthCookie.CsrfHeader))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(
            new { message = $"Falta la cabecera {AuthCookie.CsrfHeader} para las peticiones autenticadas por cookie." });
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();
