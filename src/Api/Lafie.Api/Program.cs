using Lafie.Eventing;
using Lafie.Identity.Api;
using Lafie.Persistence;
using Lafie.Persistence.Migrations;
using Lafie.Interop.Api;
using Lafie.Organization.Api;
using Lafie.Patient.Api;
using Lafie.Terminology.Api;
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

var app = builder.Build();

// Renvoie un ProblemDetails sur exception non gérée.
app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference();

// Endpoints des modules.
app.MapOrganizationEndpoints();
app.MapBuildingEndpoints();
app.MapFloorEndpoints();
app.MapSpaceEndpoints();
app.MapServiceEndpoints();
app.MapCareUnitEndpoints();
app.MapReferenceEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok", phase = "0-skeleton" }));

app.MapGet("/health/db", async (DatabaseHealth db, CancellationToken ct) =>
    await db.IsReachableAsync(ct)
        ? Results.Ok(new { database = "up" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.Run();
