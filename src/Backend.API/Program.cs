using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Backend.API.Data;
using Backend.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var db = builder.Configuration.GetSection("Database");
var sslMode = db["SslMode"];
var connectionString = $"Host={db["Host"]};Port={db["Port"]};Database={db["Name"]};Username={db["User"]};Password={db["Password"]}"
    + (string.IsNullOrEmpty(sslMode) ? "" : $";SslMode={sslMode}");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key no está configurado. En desarrollo: dotnet user-secrets set \"Jwt:Key\" <clave>; en producción: variable de entorno Jwt__Key.");
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(subject, out var userId))
                {
                    context.Fail("El token no contiene un identificador de usuario válido.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var active = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => (bool?)u.Active)
                    .SingleOrDefaultAsync();

                if (active is null)
                {
                    context.Fail("El usuario del token ya no existe.");
                }
                else if (active == false)
                {
                    context.Fail("El usuario del token está inactivo.");
                }
            }
        };
    });
builder.Services.AddAuthorization();

var authPermitLimit = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 30);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (corsOrigins is { Length: > 0 })
        {
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

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
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
