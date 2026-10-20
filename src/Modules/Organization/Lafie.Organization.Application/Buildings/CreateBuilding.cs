using Lafie.Core.Common;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Buildings;

/// <summary>Crée un bâtiment dans un établissement.</summary>
public sealed record CreateBuildingCommand(
    Guid FacilityId, string Code, string Name, string? Description = null, string? Location = null)
    : IRequest<Result<Guid>>;

public sealed class CreateBuildingHandler(
    IBuildingRepository buildings,
    IOrganizationRepository facilities) : IRequestHandler<CreateBuildingCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateBuildingCommand command, CancellationToken cancellationToken)
    {
        if (await facilities.GetByIdAsync(command.FacilityId, cancellationToken) is null)
            return Result.Failure<Guid>("Établissement introuvable.");

        var created = Building.Create(command.FacilityId, command.Code, command.Name, command.Description, command.Location);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        if (await buildings.CodeExistsAsync(command.FacilityId, created.Value.Code, null, cancellationToken))
            return Result.Failure<Guid>($"Le code '{created.Value.Code}' est déjà utilisé dans cet établissement.");

        await buildings.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
