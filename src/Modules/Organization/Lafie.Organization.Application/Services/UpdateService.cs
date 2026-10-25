using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Services;

/// <summary>Met à jour un service (y compris son rattachement à un service parent).</summary>
public sealed record UpdateServiceCommand(
    Guid Id, Guid? ParentServiceId, string Code, string Name, string Category,
    string? Description = null, string? Head = null, int DisplayOrder = 0)
    : IRequest<Result>;

public sealed class UpdateServiceHandler(IServiceRepository services)
    : IRequestHandler<UpdateServiceCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateServiceCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ServiceCategory>(command.Category, ignoreCase: true, out var category))
            return Result.Failure($"Catégorie de service inconnue : '{command.Category}'.");

        var service = await services.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Service {command.Id} introuvable.");

        var parentId = command.ParentServiceId == Guid.Empty ? null : command.ParentServiceId;
        if (parentId is { } p)
        {
            var parent = await services.GetByIdAsync(p, cancellationToken);
            if (parent is null) return Result.Failure("Service parent introuvable.");
            if (parent.FacilityId != service.FacilityId)
                return Result.Failure("Le service parent appartient à un autre établissement.");

            // Garde anti-cycle : remonter la chaîne des parents ; interdire si on retombe sur ce service.
            var cursor = parent.ParentServiceId;
            while (cursor is { } c)
            {
                if (c == service.Id) return Result.Failure("Rattachement impossible : cela créerait un cycle de services.");
                cursor = (await services.GetByIdAsync(c, cancellationToken))?.ParentServiceId;
            }
        }

        var updated = service.Update(parentId, command.Code, command.Name, category, command.Description, command.Head, command.DisplayOrder);
        if (updated.IsFailure) return updated;

        if (await services.CodeExistsAsync(service.FacilityId, service.Code, service.Id, cancellationToken))
            return Result.Failure($"Le code '{service.Code}' est déjà utilisé dans cet établissement.");

        await services.UpdateAsync(service, cancellationToken);
        return Result.Success();
    }
}
