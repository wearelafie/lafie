using Lafie.Core.Common;
using Lafie.Organization.Domain;
using Mediator;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Crée une organisation.</summary>
public sealed record CreateOrganizationCommand(
    string Name,
    string Type,
    string? Code = null,
    Guid? ParentId = null) : IRequest<Result<Guid>>;

public sealed class CreateOrganizationHandler(IOrganizationRepository repository)
    : IRequestHandler<CreateOrganizationCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrganizationType>(command.Type, ignoreCase: true, out var type))
            return Result.Failure<Guid>($"Type d'organisation inconnu : '{command.Type}'.");

        var created = DomainOrganization.Create(command.Name, type, command.Code, command.ParentId);
        if (created.IsFailure)
            return Result.Failure<Guid>(created.Error!);

        await repository.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
