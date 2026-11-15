using Lafie.Core.Common;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Planifie un bio-nettoyage d'un espace et sème la checklist du protocole.</summary>
public sealed record CreateCleaningTaskCommand(
    Guid FacilityId, Guid SpaceId, string Protocol, string Trigger, string? RequestedBy, string? Notes) : IRequest<Result<Guid>>;

public sealed class CreateCleaningTaskHandler(
    ICleaningTaskRepository tasks,
    ISpaceRepository spaces) : IRequestHandler<CreateCleaningTaskCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateCleaningTaskCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CleaningProtocol>(command.Protocol, ignoreCase: true, out var protocol))
            return Result.Failure<Guid>($"Protocole inconnu : '{command.Protocol}'.");
        if (!Enum.TryParse<CleaningTrigger>(command.Trigger, ignoreCase: true, out var trigger))
            return Result.Failure<Guid>($"Déclencheur inconnu : '{command.Trigger}'.");

        var space = await spaces.GetByIdAsync(command.SpaceId, cancellationToken);
        if (space is null || space.FacilityId != command.FacilityId)
            return Result.Failure<Guid>("Espace cible introuvable.");

        var created = CleaningTask.Create(command.FacilityId, command.SpaceId, protocol, trigger, command.RequestedBy, command.Notes);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        var template = CleaningProtocolTemplates.For(protocol);
        var items = new List<CleaningTaskItem>(template.Count);
        for (var i = 0; i < template.Count; i++)
        {
            var item = CleaningTaskItem.Create(created.Value.Id, template[i], i);
            if (item.IsFailure) return Result.Failure<Guid>(item.Error!);
            items.Add(item.Value);
        }

        await tasks.AddAsync(created.Value, items, cancellationToken);

        // Bascule l'espace en nettoyage, sauf s'il est déjà verrouillé par une maintenance (priorité à Blocked).
        var toLock = await spaces.FindAsync(command.SpaceId, cancellationToken);
        if (toLock is not null && toLock.OperationalState != SpaceOperationalState.Blocked
            && toLock.OperationalState != SpaceOperationalState.Cleaning)
        {
            toLock.SetOperationalState(SpaceOperationalState.Cleaning);
            await spaces.UpdateAsync(toLock, cancellationToken);
        }

        return Result.Success(created.Value.Id);
    }
}
