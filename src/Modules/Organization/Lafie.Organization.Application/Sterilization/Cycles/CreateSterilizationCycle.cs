using Lafie.Core.Common;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Cycles;

/// <summary>Planifie un cycle d'autoclave.</summary>
public sealed record CreateSterilizationCycleCommand(
    Guid FacilityId, string Autoclave, string? CycleNumber, string Program, string? Operator) : IRequest<Result<Guid>>;

public sealed class CreateSterilizationCycleHandler(ISterilizationCycleRepository cycles)
    : IRequestHandler<CreateSterilizationCycleCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateSterilizationCycleCommand command, CancellationToken cancellationToken)
    {
        var created = SterilizationCycle.Create(command.FacilityId, command.Autoclave, command.CycleNumber, command.Program, command.Operator);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        await cycles.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
