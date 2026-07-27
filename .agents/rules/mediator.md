# Règle — CQRS avec Mediator (PAS MediatR)

On utilise **Mediator** de *martinothamar* (source-generated, zéro réflexion runtime). Packages : `Mediator.Abstractions` (dans `Lafie.Shared`) + `Mediator.SourceGenerator` (dans le host). Enregistrement : `builder.Services.AddMediator()` (généré, découvre les handlers des assemblies référencées).

⚠️ **Ne jamais** introduire `MediatR` ni son API.

## Commands / Queries

```csharp
// Application/Establishments/RegisterEstablishment.cs
public sealed record RegisterEstablishmentCommand(string Name, string Kind) : IRequest<Guid>;

internal sealed class RegisterEstablishmentHandler(IOrganizationRepository repo)
    : IRequestHandler<RegisterEstablishmentCommand, Guid>
{
    public async ValueTask<Guid> Handle(RegisterEstablishmentCommand cmd, CancellationToken ct)
    {
        var e = Establishment.Create(cmd.Name, cmd.Kind);
        return await repo.AddAsync(e, ct);
    }
}
```

- `IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>` viennent de **Mediator.Abstractions**.
- `Handle` retourne **`ValueTask<T>`** (Mediator), pas `Task<T>`.
- Query = même chose en lecture (aucun effet de bord).

## Endpoint (module Api)

```csharp
group.MapPost("/", async (RegisterEstablishmentCommand cmd, IMediator mediator, CancellationToken ct) =>
    Results.Ok(await mediator.Send(cmd, ct)));
```

## Behaviors (pipeline)

`IPipelineBehavior<TMessage, TResponse>` (Mediator). Validation (FluentValidation), logging, etc. → à loger dans `Lafie.Web` et enregistrer côté host. (À câbler quand FluentValidation est ajouté au backend.)
