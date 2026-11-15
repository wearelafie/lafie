using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>Pièce détachée remplacée lors d'une intervention (traçabilité + coût).</summary>
public sealed class MaintenanceTicketPart : BaseEntity<Guid>
{
    public const int LabelMaxLength = 200;

    public Guid TicketId { get; private set; }
    public string Label { get; private set; }
    public string? Reference { get; private set; }
    public int Quantity { get; private set; }
    public decimal? UnitCost { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }

    private MaintenanceTicketPart(Guid id, Guid ticketId, string label, string? reference, int quantity, decimal? unitCost, DateTimeOffset createdOnUtc)
    {
        Id = id;
        TicketId = ticketId;
        Label = label;
        Reference = reference;
        Quantity = quantity;
        UnitCost = unitCost;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<MaintenanceTicketPart> Create(Guid ticketId, string label, string? reference, int quantity, decimal? unitCost)
    {
        if (ticketId == Guid.Empty) return Result.Failure<MaintenanceTicketPart>("Ticket obligatoire.");
        if (string.IsNullOrWhiteSpace(label)) return Result.Failure<MaintenanceTicketPart>("Libellé de la pièce obligatoire.");
        label = label.Trim();
        if (label.Length > LabelMaxLength) return Result.Failure<MaintenanceTicketPart>($"Le libellé dépasse {LabelMaxLength} caractères.");
        if (quantity < 1) return Result.Failure<MaintenanceTicketPart>("La quantité doit être au moins 1.");
        if (unitCost is < 0) return Result.Failure<MaintenanceTicketPart>("Le coût ne peut pas être négatif.");

        return Result.Success(new MaintenanceTicketPart(
            Guid.NewGuid(), ticketId, label, string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(), quantity, unitCost, DateTimeOffset.UtcNow));
    }

    public static MaintenanceTicketPart Rehydrate(Guid id, Guid ticketId, string label, string? reference, int quantity, decimal? unitCost, DateTimeOffset createdOnUtc)
        => new(id, ticketId, label, reference, quantity, unitCost, createdOnUtc);
}
