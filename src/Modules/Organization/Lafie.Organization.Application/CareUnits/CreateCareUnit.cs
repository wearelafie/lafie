using Lafie.Core.Common;
using Lafie.Organization.Application.Services;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.CareUnits;

/// <summary>Crée une unité de soins dans un service. L'établissement est dérivé du service (cohérence garantie).</summary>
public sealed record CreateCareUnitCommand(
    Guid ServiceId, string Code, string Name, string Type,
    int? BedCapacity = null, string? Description = null, int DisplayOrder = 0)
    : IRequest<Result<Guid>>;

public sealed class CreateCareUnitHandler(
    ICareUnitRepository careUnits,
    IServiceRepository services) : IRequestHandler<CreateCareUnitCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateCareUnitCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CareUnitType>(command.Type, ignoreCase: true, out var type))
            return Result.Failure<Guid>($"Type d'unité inconnu : '{command.Type}'.");

        var service = await services.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null) return Result.Failure<Guid>("Service introuvable.");

        var created = CareUnit.Create(
            service.FacilityId, command.ServiceId, command.Code, command.Name, type,
            command.BedCapacity, command.Description, command.DisplayOrder);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        if (await careUnits.CodeExistsAsync(service.FacilityId, created.Value.Code, null, cancellationToken))
            return Result.Failure<Guid>($"Le code '{created.Value.Code}' est déjà utilisé dans cet établissement.");

        await careUnits.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
