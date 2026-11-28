using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Purchasing;

/// <summary>Référence un nouveau fournisseur dans l'établissement.</summary>
public sealed record CreateSupplierCommand(
    Guid FacilityId, string Code, string Name, string? ContactName, string? Email,
    string? Phone, string? Address, string? Notes) : IRequest<Result<Guid>>;

public sealed class CreateSupplierHandler(ISupplierRepository suppliers)
    : IRequestHandler<CreateSupplierCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        if (await suppliers.CodeExistsAsync(command.FacilityId, command.Code.Trim(), cancellationToken))
            return Result.Failure<Guid>($"Un fournisseur porte déjà le code « {command.Code} » dans cet établissement.");

        var created = Supplier.Create(command.FacilityId, command.Code, command.Name, command.ContactName,
            command.Email, command.Phone, command.Address, command.Notes);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        await suppliers.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}

/// <summary>Modifie un fournisseur (le code est immuable).</summary>
public sealed record UpdateSupplierCommand(
    Guid Id, string Name, string? ContactName, string? Email, string? Phone, string? Address, string? Notes)
    : IRequest<Result>;

public sealed class UpdateSupplierHandler(ISupplierRepository suppliers)
    : IRequestHandler<UpdateSupplierCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        var supplier = await suppliers.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Fournisseur {command.Id} introuvable.");

        var result = supplier.Edit(command.Name, command.ContactName, command.Email, command.Phone, command.Address, command.Notes);
        if (result.IsFailure) return result;

        await suppliers.UpdateAsync(supplier, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Active ou désactive un fournisseur.</summary>
public sealed record SetSupplierStatusCommand(Guid Id, bool Active) : IRequest<Result>;

public sealed class SetSupplierStatusHandler(ISupplierRepository suppliers)
    : IRequestHandler<SetSupplierStatusCommand, Result>
{
    public async ValueTask<Result> Handle(SetSupplierStatusCommand command, CancellationToken cancellationToken)
    {
        var supplier = await suppliers.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Fournisseur {command.Id} introuvable.");

        supplier.SetActive(command.Active);
        await suppliers.UpdateAsync(supplier, cancellationToken);
        return Result.Success();
    }
}
