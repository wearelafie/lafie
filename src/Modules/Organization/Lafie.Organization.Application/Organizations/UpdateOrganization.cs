using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Met à jour un établissement.</summary>
public sealed record UpdateOrganizationCommand(Guid Id, OrganizationWriteModel Model) : IRequest<Result>;

public sealed class UpdateOrganizationHandler(IOrganizationRepository repository)
    : IRequestHandler<UpdateOrganizationCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var org = await repository.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Établissement {command.Id} introuvable.");

        var m = command.Model;
        var result = org.Update(
            m.Code, m.OfficialName, m.FacilityTypeCode, m.OwnershipTypeCode, m.CountryCode,
            m.Identity(), m.Address(), m.Legal(), m.Operations(), m.Contact());
        if (result.IsFailure) return result;

        if (await repository.CodeExistsAsync(org.Code, org.Id, cancellationToken))
            return Result.Failure($"Le code '{org.Code}' est déjà utilisé.");

        await repository.UpdateAsync(org, cancellationToken);
        return Result.Success();
    }
}
