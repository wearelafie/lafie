using Lafie.Core.Common;
using Mediator;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Crée un établissement.</summary>
public sealed record CreateOrganizationCommand(OrganizationWriteModel Model) : IRequest<Result<Guid>>;

public sealed class CreateOrganizationHandler(IOrganizationRepository repository)
    : IRequestHandler<CreateOrganizationCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var m = command.Model;
        var created = DomainOrganization.Create(
            m.Code, m.OfficialName, m.FacilityTypeCode, m.OwnershipTypeCode, m.CountryCode,
            m.Identity(), m.Address(), m.Legal(), m.Operations(), m.Contact());
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        if (await repository.CodeExistsAsync(created.Value.Code, null, cancellationToken))
            return Result.Failure<Guid>($"Le code '{created.Value.Code}' est déjà utilisé.");

        await repository.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
