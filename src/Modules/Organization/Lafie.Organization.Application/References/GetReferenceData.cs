using Mediator;

namespace Lafie.Organization.Application.References;

/// <summary>Valeurs d'une catégorie de référentiel (ex. FACILITY_TYPE).</summary>
public sealed record GetReferenceValuesQuery(string Category) : IRequest<IReadOnlyList<ReferenceValueDto>>;

public sealed class GetReferenceValuesHandler(IReferenceDataRepository repository)
    : IRequestHandler<GetReferenceValuesQuery, IReadOnlyList<ReferenceValueDto>>
{
    public async ValueTask<IReadOnlyList<ReferenceValueDto>> Handle(GetReferenceValuesQuery query, CancellationToken cancellationToken)
        => await repository.GetReferenceValuesAsync(query.Category, cancellationToken);
}

/// <summary>Divisions administratives d'un niveau, filtrées par parent (code) si fourni.</summary>
public sealed record GetGeoDivisionsQuery(string Level, string? ParentCode) : IRequest<IReadOnlyList<GeoDivisionDto>>;

public sealed class GetGeoDivisionsHandler(IReferenceDataRepository repository)
    : IRequestHandler<GetGeoDivisionsQuery, IReadOnlyList<GeoDivisionDto>>
{
    public async ValueTask<IReadOnlyList<GeoDivisionDto>> Handle(GetGeoDivisionsQuery query, CancellationToken cancellationToken)
        => await repository.GetGeoDivisionsAsync(query.Level, query.ParentCode, cancellationToken);
}
