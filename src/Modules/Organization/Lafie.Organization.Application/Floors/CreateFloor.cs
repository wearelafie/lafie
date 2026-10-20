using Lafie.Core.Common;
using Lafie.Organization.Application.Buildings;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Floors;

/// <summary>Crée un étage dans un bâtiment.</summary>
public sealed record CreateFloorCommand(
    Guid BuildingId, string Code, string Name, int? Level = null, int DisplayOrder = 0, string? Description = null)
    : IRequest<Result<Guid>>;

public sealed class CreateFloorHandler(
    IFloorRepository floors,
    IBuildingRepository buildings) : IRequestHandler<CreateFloorCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateFloorCommand command, CancellationToken cancellationToken)
    {
        if (await buildings.GetByIdAsync(command.BuildingId, cancellationToken) is null)
            return Result.Failure<Guid>("Bâtiment introuvable.");

        var created = Floor.Create(command.BuildingId, command.Code, command.Name, command.Level, command.DisplayOrder, command.Description);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        if (await floors.CodeExistsAsync(command.BuildingId, created.Value.Code, null, cancellationToken))
            return Result.Failure<Guid>($"Le code '{created.Value.Code}' est déjà utilisé dans ce bâtiment.");

        await floors.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
