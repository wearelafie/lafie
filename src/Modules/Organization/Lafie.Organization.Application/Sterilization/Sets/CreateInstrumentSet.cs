using Lafie.Core.Common;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Sets;

/// <summary>Enregistre une nouvelle boîte d'instruments au catalogue CSSD (état initial : souillée).</summary>
public sealed record CreateInstrumentSetCommand(
    Guid FacilityId, string Code, string Name, string? Description, string? Category, int ItemCount) : IRequest<Result<Guid>>;

public sealed class CreateInstrumentSetHandler(IInstrumentSetRepository sets)
    : IRequestHandler<CreateInstrumentSetCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateInstrumentSetCommand command, CancellationToken cancellationToken)
    {
        if (await sets.CodeExistsAsync(command.FacilityId, command.Code.Trim(), null, cancellationToken))
            return Result.Failure<Guid>($"Une boîte avec le code '{command.Code}' existe déjà.");

        var created = InstrumentSet.Create(command.FacilityId, command.Code, command.Name, command.Description, command.Category, command.ItemCount);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        await sets.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
