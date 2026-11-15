using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Sets;

/// <summary>Réforme une boîte (sortie du circuit).</summary>
public sealed record RetireInstrumentSetCommand(Guid Id) : IRequest<Result>;

public sealed class RetireInstrumentSetHandler(IInstrumentSetRepository sets)
    : IRequestHandler<RetireInstrumentSetCommand, Result>
{
    public async ValueTask<Result> Handle(RetireInstrumentSetCommand command, CancellationToken cancellationToken)
    {
        var set = await sets.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Boîte {command.Id} introuvable.");

        var result = set.Retire();
        if (result.IsFailure) return result;

        await sets.UpdateAsync(set, cancellationToken);
        return Result.Success();
    }
}
