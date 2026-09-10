using System.Text;
using CVMatchAI.API.Data;
using CVMatchAI.API.Middleware;
using CVMatchAI.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;

// Npgsql 8 rechaza DateTime.UtcNow en columnas "timestamp without time zone"
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Render (y otros PaaS) inyectan PORT; si no hay, se queda el de launchSettings / 8080
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ─── Base de datos PostgreSQL (Supabase) ───────────────────────────
var dbCs = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection");
var npgsqlCs = new Npgsql.NpgsqlConnectionStringBuilder(dbCs)
{
    MaxAutoPrepare = 0,
    Multiplexing   = false,
    SslMode        = Npgsql.SslMode.Require,
    TrustServerCertificate = true,
};
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(npgsqlCs.ConnectionString));

// ─── Autenticación JWT ─────────────────────────────────────────────
var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "";
if (jwtKey.Length < 32)
    jwtKey = jwtKey.PadRight(32, '0');

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience            = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero
        };

        // Devolver 401 como JSON en lugar de redirigir
        options.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode  = 401;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"message\":\"No autorizado. Token inválido o expirado.\"}");
            }
        };
    });

builder.Services.AddAuthorization();

// ─── Servicios de la aplicación (con interfaces para mejor testabilidad) ───
builder.Services.AddHttpClient("gemini");
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddSingleton<IPdfService, PdfService>();
builder.Services.AddSingleton<IClaudeService, ClaudeService>();

// ─── CORS: permitir el dev server de Vite ─────────────────────────
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p
        .WithOrigins(
            "http://localhost:5173",
            "http://localhost:3000",
            "https://micvai.online",
            "https://www.micvai.online",
            "https://cvai-pied.vercel.app"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CVMatch AI API", Version = "v1" });
    // Configurar Swagger para usar JWT
    c.AddSecurityDefinition("Bearer", new()
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description  = "Ingresa el token JWT"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─── Crear tablas si no existen (EnsureCreated no sirve si public ya tiene tablas) ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var creator = db.GetService<IRelationalDatabaseCreator>();
    try
    {
        creator.CreateTables();
        app.Logger.LogInformation("Tablas de la aplicación creadas.");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "CreateTables: {Message}", ex.Message);
    }
}

// ─── Pipeline de middlewares ───────────────────────────────────────
app.UseMiddleware<ErrorHandlingMiddleware>(); // Manejo global de errores

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/db", async (AppDbContext db) =>
{
    try
    {
        var count = await db.Users.CountAsync();
        return Results.Ok(new { status = "ok", users = count });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "error", detail = ex.GetBaseException().Message }, statusCode: 500);
    }
});
app.MapControllers();

app.Run();
