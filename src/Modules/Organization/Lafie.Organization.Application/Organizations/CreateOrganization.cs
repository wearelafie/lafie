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
    string? ShortName = null,
    string? Description = null,
    string? Phone = null,
    string? Email = null,
    string? Timezone = null,
    string? City = null,
    string? Country = null,
    Guid? ParentId = null) : IRequest<Result<Guid>>;

public sealed class CreateOrganizationHandler(IOrganizationRepository repository)
    : IRequestHandler<CreateOrganizationCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrganizationType>(command.Type, ignoreCase: true, out var type))
            return Result.Failure<Guid>($"Type d'organisation inconnu : '{command.Type}'.");

        var profile = new OrganizationProfile(
            command.ShortName, command.Description, command.Phone, command.Email,
            string.IsNullOrWhiteSpace(command.Timezone) ? "Africa/Lome" : command.Timezone!,
            command.City, command.Country);

        var created = DomainOrganization.Create(command.Name, type, command.Code, profile, command.ParentId);
        if (created.IsFailure)
            return Result.Failure<Guid>(created.Error!);

        await repository.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
