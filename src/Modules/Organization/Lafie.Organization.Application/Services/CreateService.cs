using Lafie.Core.Common;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Services;

/// <summary>Crée un service dans un établissement, éventuellement rattaché à un service parent.</summary>
public sealed record CreateServiceCommand(
    Guid FacilityId, Guid? ParentServiceId, string Code, string Name, string Category,
    string? Description = null, string? Head = null, int DisplayOrder = 0)
    : IRequest<Result<Guid>>;

public sealed class CreateServiceHandler(
    IServiceRepository services,
    IOrganizationRepository facilities) : IRequestHandler<CreateServiceCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateServiceCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ServiceCategory>(command.Category, ignoreCase: true, out var category))
            return Result.Failure<Guid>($"Catégorie de service inconnue : '{command.Category}'.");

        if (await facilities.GetByIdAsync(command.FacilityId, cancellationToken) is null)
            return Result.Failure<Guid>("Établissement introuvable.");

        if (command.ParentServiceId is { } parentId && parentId != Guid.Empty)
        {
            var parent = await services.GetByIdAsync(parentId, cancellationToken);
            if (parent is null) return Result.Failure<Guid>("Service parent introuvable.");
            if (parent.FacilityId != command.FacilityId)
                return Result.Failure<Guid>("Le service parent appartient à un autre établissement.");
        }

        var created = Service.Create(
            command.FacilityId, command.ParentServiceId, command.Code, command.Name, category,
            command.Description, command.Head, command.DisplayOrder);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        if (await services.CodeExistsAsync(command.FacilityId, created.Value.Code, null, cancellationToken))
            return Result.Failure<Guid>($"Le code '{created.Value.Code}' est déjà utilisé dans cet établissement.");

        await services.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
