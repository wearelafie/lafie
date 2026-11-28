using Lafie.Core.Common;
using Mediator;

namespace Lafie.Pharmacy.Application.Catalog;

/// <summary>Détail d'une fiche médicament.</summary>
public sealed record GetMedicationQuery(Guid Id) : IRequest<Result<MedicationDto>>;

public sealed class GetMedicationHandler(IMedicationRepository medications)
    : IRequestHandler<GetMedicationQuery, Result<MedicationDto>>
{
    public async ValueTask<Result<MedicationDto>> Handle(GetMedicationQuery query, CancellationToken cancellationToken)
    {
        var medication = await medications.GetByIdAsync(query.Id, cancellationToken);
        return medication is null
            ? Result.Failure<MedicationDto>("Médicament introuvable.")
            : Result.Success(medication);
    }
}
