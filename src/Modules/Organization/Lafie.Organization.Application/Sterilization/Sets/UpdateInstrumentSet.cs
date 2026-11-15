using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Sets;

/// <summary>Modifie les informations descriptives d'une boîte.</summary>
public sealed record UpdateInstrumentSetCommand(
    Guid Id, string Name, string? Description, string? Category, int ItemCount) : IRequest<Result>;

public sealed class UpdateInstrumentSetHandler(IInstrumentSetRepository sets)
    : IRequestHandler<UpdateInstrumentSetCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateInstrumentSetCommand command, CancellationToken cancellationToken)
    {
        var set = await sets.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Boîte {command.Id} introuvable.");

        var result = set.Edit(command.Name, command.Description, command.Category, command.ItemCount);
        if (result.IsFailure) return result;

        await sets.UpdateAsync(set, cancellationToken);
        return Result.Success();
    }
}
