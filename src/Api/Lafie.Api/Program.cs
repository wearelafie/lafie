using System.Text;
using Lafie.Core.Context;
using Lafie.Eventing;
using Lafie.Identity.Api;
using Lafie.Identity.Application.Security;
using Lafie.Identity.Infrastructure;
using Lafie.Persistence;
using Lafie.Persistence.Migrations;
using Lafie.Interop.Api;
using Lafie.Organization.Api;
using Lafie.Patient.Api;
using Lafie.Terminology.Api;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Erreurs HTTP normalisées (RFC 9457 ProblemDetails) + mapping des exceptions applicatives.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<Lafie.Api.CustomExceptionHandler>();

// Documentation d'API (OpenAPI) + UI de référence Scalar.
builder.Services.AddOpenApi();

// CQRS via Mediator (source-generated) — découvre les handlers des modules référencés.
// Lifetime scoped : les handlers peuvent consommer des services scoped (repos, current user…).
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

// Persistance Dapper/Npgsql partagée.
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5433;Database=lafie;Username=lafie;Password=lafie";
builder.Services.AddLafiePersistence(connectionString);

// Migrations SQL versionnées appliquées au démarrage (scripts embarqués dans cet assembly).
builder.Services.AddLafieMigrations(typeof(Program).Assembly);

// Seed de données de développement (module Infrastructure) — uniquement hors production,
// après les migrations. Script idempotent ; sans effet en production.
if (builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<Lafie.Api.DevDataSeeder>();

// Eventing (bus in-process ; Outbox/broker à venir).
builder.Services.AddLafieEventing();

// Enregistrement des modules — Phase 0 (socle).
builder.Services
    .AddIdentityModule()
    .AddOrganizationModule()
    .AddPatientModule()
    .AddTerminologyModule()
    .AddInteropModule();

// --- Authentification / autorisation (JWT Bearer émis par le module Identity) ---
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
    // Clé de dev par défaut (≥ 32 octets). À surcharger via config/secret en production.
    jwtOptions.SigningKey = "lafie-dev-signing-key-change-me-0123456789";
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IJwtTokenService>(_ => new JwtTokenService(jwtOptions));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<IEstablishmentGuard, EstablishmentGuard>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Par défaut tout endpoint exige un utilisateur authentifié ; /auth/login, /health et la doc sont en AllowAnonymous.
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
// Policies dynamiques "perm:<code>" (RBAC fin).
builder.Services.AddLafiePermissionAuthorization();

var app = builder.Build();

// Renvoie un ProblemDetails sur exception non gérée.
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference().AllowAnonymous();

// Authentification.
app.MapAuthEndpoints();

// Endpoints des modules.
app.MapOrganizationEndpoints();
app.MapBuildingEndpoints();
app.MapFloorEndpoints();
app.MapSpaceEndpoints();
app.MapServiceEndpoints();
app.MapCareUnitEndpoints();
app.MapReferenceEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok", phase = "0-skeleton" })).AllowAnonymous();

app.MapGet("/health/db", async (DatabaseHealth db, CancellationToken ct) =>
    await db.IsReachableAsync(ct)
        ? Results.Ok(new { database = "up" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable)).AllowAnonymous();

app.Run();
