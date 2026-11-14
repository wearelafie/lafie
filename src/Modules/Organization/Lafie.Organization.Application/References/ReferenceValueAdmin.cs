using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.References;

// ----------------------------------------------------------------------- Requêtes

/// <summary>Catégories de référentiel disponibles (avec nombre de codes).</summary>
public sealed record GetReferenceCategoriesQuery : IRequest<IReadOnlyList<ReferenceCategoryDto>>;

public sealed class GetReferenceCategoriesHandler(IReferenceDataRepository repository)
    : IRequestHandler<GetReferenceCategoriesQuery, IReadOnlyList<ReferenceCategoryDto>>
{
    public async ValueTask<IReadOnlyList<ReferenceCategoryDto>> Handle(GetReferenceCategoriesQuery query, CancellationToken cancellationToken)
        => await repository.GetReferenceCategoriesAsync(cancellationToken);
}

/// <summary>Valeurs d'une catégorie pour l'administration (inclut inactifs si demandé).</summary>
public sealed record GetReferenceValuesAdminQuery(string Category, string? Search, bool IncludeInactive)
    : IRequest<IReadOnlyList<ReferenceValueAdminDto>>;

public sealed class GetReferenceValuesAdminHandler(IReferenceDataRepository repository)
    : IRequestHandler<GetReferenceValuesAdminQuery, IReadOnlyList<ReferenceValueAdminDto>>
{
    public async ValueTask<IReadOnlyList<ReferenceValueAdminDto>> Handle(GetReferenceValuesAdminQuery query, CancellationToken cancellationToken)
        => await repository.GetReferenceValuesAdminAsync(query.Category.Trim().ToUpperInvariant(), query.Search?.Trim(), query.IncludeInactive, cancellationToken);
}

// ----------------------------------------------------------------------- Commandes

public sealed record CreateReferenceValueCommand(string Category, string Code, string LabelFr, string LabelEn, int DisplayOrder, bool Active)
    : IRequest<Result>;

public sealed class CreateReferenceValueHandler(IReferenceDataRepository repository)
    : IRequestHandler<CreateReferenceValueCommand, Result>
{
    public async ValueTask<Result> Handle(CreateReferenceValueCommand command, CancellationToken cancellationToken)
    {
        var v = Normalize(command.Category, command.Code, command.LabelFr, command.LabelEn);
        if (v.IsFailure) return v;
        var (category, code, labelFr, labelEn) = v.Value;

        if (await repository.ReferenceValueCodeTakenAsync(category, code, cancellationToken))
            return Result.Failure($"Le code '{code}' existe déjà dans la catégorie '{category}'.");

        await repository.CreateReferenceValueAsync(category, code, labelFr, labelEn, command.DisplayOrder, command.Active, cancellationToken);
        return Result.Success();
    }

    internal static Result<(string Category, string Code, string LabelFr, string LabelEn)> Normalize(string category, string code, string labelFr, string labelEn)
    {
        category = (category ?? string.Empty).Trim().ToUpperInvariant();
        code = (code ?? string.Empty).Trim().ToUpperInvariant();
        labelFr = (labelFr ?? string.Empty).Trim();
        labelEn = (labelEn ?? string.Empty).Trim();
        if (category.Length is 0 or > 50) return Result.Failure<(string, string, string, string)>("La catégorie est obligatoire (max 50).");
        if (code.Length is 0 or > 50) return Result.Failure<(string, string, string, string)>("Le code est obligatoire (max 50).");
        if (labelFr.Length is 0 or > 200) return Result.Failure<(string, string, string, string)>("Le libellé FR est obligatoire (max 200).");
        if (labelEn.Length is 0 or > 200) return Result.Failure<(string, string, string, string)>("Le libellé EN est obligatoire (max 200).");
        return Result.Success((category, code, labelFr, labelEn));
    }
}

public sealed record UpdateReferenceValueCommand(string Category, string Code, string LabelFr, string LabelEn, int DisplayOrder, bool Active)
    : IRequest<Result>;

public sealed class UpdateReferenceValueHandler(IReferenceDataRepository repository)
    : IRequestHandler<UpdateReferenceValueCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateReferenceValueCommand command, CancellationToken cancellationToken)
    {
        var v = CreateReferenceValueHandler.Normalize(command.Category, command.Code, command.LabelFr, command.LabelEn);
        if (v.IsFailure) return v;
        var (category, code, labelFr, labelEn) = v.Value;

        var ok = await repository.UpdateReferenceValueAsync(category, code, labelFr, labelEn, command.DisplayOrder, command.Active, cancellationToken);
        return ok ? Result.Success() : Result.Failure("Code de référence introuvable.");
    }
}

public sealed record SetReferenceValueActiveCommand(string Category, string Code, bool Active) : IRequest<Result>;

public sealed class SetReferenceValueActiveHandler(IReferenceDataRepository repository)
    : IRequestHandler<SetReferenceValueActiveCommand, Result>
{
    public async ValueTask<Result> Handle(SetReferenceValueActiveCommand command, CancellationToken cancellationToken)
    {
        var ok = await repository.SetReferenceValueActiveAsync(
            command.Category.Trim().ToUpperInvariant(), command.Code.Trim().ToUpperInvariant(), command.Active, cancellationToken);
        return ok ? Result.Success() : Result.Failure("Code de référence introuvable.");
    }
}

public sealed record DeleteReferenceValueCommand(string Category, string Code) : IRequest<Result>;

public sealed class DeleteReferenceValueHandler(IReferenceDataRepository repository)
    : IRequestHandler<DeleteReferenceValueCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteReferenceValueCommand command, CancellationToken cancellationToken)
    {
        var ok = await repository.DeleteReferenceValueAsync(
            command.Category.Trim().ToUpperInvariant(), command.Code.Trim().ToUpperInvariant(), cancellationToken);
        return ok ? Result.Success() : Result.Failure("Code de référence introuvable.");
    }
}
