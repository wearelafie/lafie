using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.References;

/// <summary>Niveaux de division administrative supportés (ordre hiérarchique).</summary>
public static class GeoLevels
{
    public static readonly string[] All = ["Country", "Region", "Prefecture", "Commune"];
    public static string? ParentOf(string level) => level switch
    {
        "Region" => "Country",
        "Prefecture" => "Region",
        "Commune" => "Prefecture",
        _ => null, // Country : racine.
    };
}

public sealed record CreateGeoDivisionCommand(string Level, string Code, string Name, string? ParentCode, int DisplayOrder)
    : IRequest<Result<Guid>>;

public sealed class CreateGeoDivisionHandler(IReferenceDataRepository repository)
    : IRequestHandler<CreateGeoDivisionCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateGeoDivisionCommand command, CancellationToken cancellationToken)
    {
        var level = (command.Level ?? string.Empty).Trim();
        var code = (command.Code ?? string.Empty).Trim().ToUpperInvariant();
        var name = (command.Name ?? string.Empty).Trim();
        if (!GeoLevels.All.Contains(level)) return Result.Failure<Guid>($"Niveau invalide : '{level}'.");
        if (code.Length is 0 or > 50) return Result.Failure<Guid>("Le code est obligatoire (max 50).");
        if (name.Length is 0 or > 200) return Result.Failure<Guid>("Le nom est obligatoire (max 200).");
        if (await repository.GeoDivisionCodeTakenAsync(code, cancellationToken))
            return Result.Failure<Guid>($"Le code '{code}' est déjà utilisé.");

        Guid? parentId = null;
        var expectedParent = GeoLevels.ParentOf(level);
        if (expectedParent is not null)
        {
            if (string.IsNullOrWhiteSpace(command.ParentCode))
                return Result.Failure<Guid>($"Un parent de niveau {expectedParent} est requis.");
            parentId = await repository.GetGeoDivisionIdByCodeAsync(command.ParentCode.Trim().ToUpperInvariant(), cancellationToken);
            if (parentId is null) return Result.Failure<Guid>($"Parent '{command.ParentCode}' introuvable.");
        }

        var id = await repository.CreateGeoDivisionAsync(level, code, name, parentId, command.DisplayOrder, cancellationToken);
        return Result.Success(id);
    }
}

public sealed record UpdateGeoDivisionCommand(Guid Id, string Name, string? ParentCode, int DisplayOrder) : IRequest<Result>;

public sealed class UpdateGeoDivisionHandler(IReferenceDataRepository repository)
    : IRequestHandler<UpdateGeoDivisionCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateGeoDivisionCommand command, CancellationToken cancellationToken)
    {
        var name = (command.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > 200) return Result.Failure("Le nom est obligatoire (max 200).");

        Guid? parentId = null;
        if (!string.IsNullOrWhiteSpace(command.ParentCode))
        {
            parentId = await repository.GetGeoDivisionIdByCodeAsync(command.ParentCode.Trim().ToUpperInvariant(), cancellationToken);
            if (parentId is null) return Result.Failure($"Parent '{command.ParentCode}' introuvable.");
            if (parentId == command.Id) return Result.Failure("Une division ne peut pas être son propre parent.");
        }

        var ok = await repository.UpdateGeoDivisionAsync(command.Id, name, parentId, command.DisplayOrder, cancellationToken);
        return ok ? Result.Success() : Result.Failure("Division introuvable.");
    }
}

public sealed record DeleteGeoDivisionCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteGeoDivisionHandler(IReferenceDataRepository repository)
    : IRequestHandler<DeleteGeoDivisionCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteGeoDivisionCommand command, CancellationToken cancellationToken)
    {
        var children = await repository.CountGeoChildrenAsync(command.Id, cancellationToken);
        if (children > 0) return Result.Failure($"Suppression impossible : {children} division(s) enfant(s) rattachée(s).");

        var ok = await repository.DeleteGeoDivisionAsync(command.Id, cancellationToken);
        return ok ? Result.Success() : Result.Failure("Division introuvable.");
    }
}
