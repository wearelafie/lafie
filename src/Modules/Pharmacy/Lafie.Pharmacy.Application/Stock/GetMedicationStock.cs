using Lafie.Core.Common;
using Lafie.Pharmacy.Application.Catalog;
using Mediator;

namespace Lafie.Pharmacy.Application.Stock;

/// <summary>Détail de stock d'un médicament : ses lots et ses derniers mouvements.</summary>
public sealed record MedicationStockDto(
    MedicationDto Medication, int OnHand, int ReorderThreshold,
    IReadOnlyList<StockLotDto> Lots, IReadOnlyList<StockMovementDto> Movements);

public sealed record GetMedicationStockQuery(Guid FacilityId, Guid MedicationId)
    : IRequest<Result<MedicationStockDto>>;

public sealed class GetMedicationStockHandler(IStockRepository stock, IMedicationRepository medications)
    : IRequestHandler<GetMedicationStockQuery, Result<MedicationStockDto>>
{
    private const int MovementsLimit = 50;

    public async ValueTask<Result<MedicationStockDto>> Handle(GetMedicationStockQuery query, CancellationToken cancellationToken)
    {
        var medication = await medications.GetByIdAsync(query.MedicationId, cancellationToken);
        if (medication is null || medication.FacilityId != query.FacilityId)
            return Result.Failure<MedicationStockDto>("Médicament introuvable dans cet établissement.");

        var lots = await stock.GetLotsAsync(query.FacilityId, query.MedicationId, cancellationToken);
        var movements = await stock.GetMovementsAsync(query.FacilityId, query.MedicationId, MovementsLimit, cancellationToken);
        var item = await stock.FindItemAsync(query.FacilityId, query.MedicationId, cancellationToken);
        var onHand = lots.Sum(l => l.QuantityOnHand);

        return Result.Success(new MedicationStockDto(
            medication, onHand, item?.ReorderThreshold ?? 0, lots, movements));
    }
}
