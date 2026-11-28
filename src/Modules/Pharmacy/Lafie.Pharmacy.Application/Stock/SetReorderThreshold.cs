using Lafie.Core.Common;
using Lafie.Pharmacy.Application.Catalog;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Stock;

/// <summary>Définit le seuil de réapprovisionnement d'un médicament (crée la fiche de stock au besoin).</summary>
public sealed record SetReorderThresholdCommand(Guid FacilityId, Guid MedicationId, int Threshold, string? Notes)
    : IRequest<Result>;

public sealed class SetReorderThresholdHandler(IStockRepository stock, IMedicationRepository medications)
    : IRequestHandler<SetReorderThresholdCommand, Result>
{
    public async ValueTask<Result> Handle(SetReorderThresholdCommand command, CancellationToken cancellationToken)
    {
        var medication = await medications.GetByIdAsync(command.MedicationId, cancellationToken);
        if (medication is null || medication.FacilityId != command.FacilityId)
            return Result.Failure("Médicament introuvable dans cet établissement.");

        var item = await stock.FindItemAsync(command.FacilityId, command.MedicationId, cancellationToken);
        if (item is null)
        {
            var created = StockItem.Create(command.FacilityId, command.MedicationId, command.Threshold, command.Notes);
            if (created.IsFailure) return Result.Failure(created.Error!);
            await stock.AddItemAsync(created.Value, cancellationToken);
            return Result.Success();
        }

        var result = item.SetReorderThreshold(command.Threshold, command.Notes);
        if (result.IsFailure) return result;
        await stock.UpdateItemAsync(item, cancellationToken);
        return Result.Success();
    }
}
