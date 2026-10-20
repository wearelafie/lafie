using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Met à jour le nom et le profil d'une organisation.</summary>
public sealed record UpdateOrganizationCommand(
    Guid Id,
    string Name,
    string? ShortName = null,
    string? Description = null,
    string? Phone = null,
    string? Email = null,
    string? Timezone = null,
    string? City = null,
    string? Country = null) : IRequest<Result>;

public sealed class UpdateOrganizationHandler(IOrganizationRepository repository)
    : IRequestHandler<UpdateOrganizationCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var org = await repository.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Organisation {command.Id} introuvable.");

        var profile = new OrganizationProfile(
            command.ShortName, command.Description, command.Phone, command.Email,
            string.IsNullOrWhiteSpace(command.Timezone) ? "Africa/Lome" : command.Timezone!,
            command.City, command.Country);

        var result = org.Update(command.Name, profile);
        if (result.IsFailure) return result;

        await repository.UpdateAsync(org, cancellationToken);
        return Result.Success();
    }
}
