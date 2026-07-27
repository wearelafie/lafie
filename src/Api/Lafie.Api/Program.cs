using Lafie.Eventing;
using Lafie.Identity.Api;
using Lafie.Persistence;
using Lafie.Interop.Api;
using Lafie.Organization.Api;
using Lafie.Patient.Api;
using Lafie.Terminology.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Erreurs HTTP normalisées (RFC 9457 ProblemDetails).
builder.Services.AddProblemDetails();

// Documentation d'API (OpenAPI) + UI de référence Scalar.
builder.Services.AddOpenApi();

// CQRS via Mediator (source-generated) — découvre les handlers des modules référencés.
builder.Services.AddMediator();

// Persistance Dapper/Npgsql partagée.
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5433;Database=lafie;Username=lafie;Password=lafie";
builder.Services.AddLafiePersistence(connectionString);

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

app.MapGet("/health", () => Results.Ok(new { status = "ok", phase = "0-skeleton" }));

app.MapGet("/health/db", async (DatabaseHealth db, CancellationToken ct) =>
    await db.IsReachableAsync(ct)
        ? Results.Ok(new { database = "up" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.Run();
