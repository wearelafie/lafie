using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>
/// Cycle d'autoclave (CSSD) : un passage de stérilisation avec sa charge de boîtes, ses paramètres et son résultat.
/// La propagation du résultat aux boîtes de la charge est orchestrée par le handler.
/// </summary>
public sealed class SterilizationCycle : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 2000;

    public Guid FacilityId { get; private set; }
    public string Autoclave { get; private set; }
    public string? CycleNumber { get; private set; }
    public string Program { get; private set; }
    public SterilizationCycleStatus Status { get; private set; }
    public string? Operator { get; private set; }
    public decimal? TemperatureC { get; private set; }
    public decimal? PressureBar { get; private set; }
    public int? DurationMin { get; private set; }
    public DateTimeOffset? StartedOnUtc { get; private set; }
    public DateTimeOffset? EndedOnUtc { get; private set; }
    public string? ResultNotes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private SterilizationCycle(
        Guid id, Guid facilityId, string autoclave, string? cycleNumber, string program, SterilizationCycleStatus status,
        string? @operator, decimal? temperatureC, decimal? pressureBar, int? durationMin,
        DateTimeOffset? startedOnUtc, DateTimeOffset? endedOnUtc, string? resultNotes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        Autoclave = autoclave;
        CycleNumber = cycleNumber;
        Program = program;
        Status = status;
        Operator = @operator;
        TemperatureC = temperatureC;
        PressureBar = pressureBar;
        DurationMin = durationMin;
        StartedOnUtc = startedOnUtc;
        EndedOnUtc = endedOnUtc;
        ResultNotes = resultNotes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<SterilizationCycle> Create(
        Guid facilityId, string autoclave, string? cycleNumber, string program, string? @operator)
    {
        if (facilityId == Guid.Empty) return Result.Failure<SterilizationCycle>("Établissement obligatoire.");
        if (string.IsNullOrWhiteSpace(autoclave)) return Result.Failure<SterilizationCycle>("L'autoclave est obligatoire.");
        if (string.IsNullOrWhiteSpace(program)) return Result.Failure<SterilizationCycle>("Le programme est obligatoire.");

        var cycle = new SterilizationCycle(
            Guid.NewGuid(), facilityId, autoclave.Trim()[..Math.Min(autoclave.Trim().Length, NameMaxLength)],
            Trim(cycleNumber, NameMaxLength), program.Trim()[..Math.Min(program.Trim().Length, NameMaxLength)],
            SterilizationCycleStatus.Planned, Trim(@operator, NameMaxLength), null, null, null, null, null, null,
            DateTimeOffset.UtcNow, null);
        return Result.Success(cycle);
    }

    public bool IsOpen => Status is SterilizationCycleStatus.Planned or SterilizationCycleStatus.Running;

    public Result Start()
    {
        if (Status is not SterilizationCycleStatus.Planned) return Result.Failure("Seul un cycle planifié peut démarrer.");
        Status = SterilizationCycleStatus.Running;
        StartedOnUtc ??= DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    /// <summary>Clôture le cycle avec son résultat et ses paramètres mesurés.</summary>
    public Result Close(bool passed, decimal? temperatureC, decimal? pressureBar, int? durationMin, string? resultNotes)
    {
        if (!IsOpen) return Result.Failure("Le cycle est déjà clôturé.");
        Status = passed ? SterilizationCycleStatus.Passed : SterilizationCycleStatus.Failed;
        TemperatureC = temperatureC;
        PressureBar = pressureBar;
        DurationMin = durationMin;
        ResultNotes = Trim(resultNotes, TextMaxLength);
        StartedOnUtc ??= DateTimeOffset.UtcNow;
        EndedOnUtc = DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    public static SterilizationCycle Rehydrate(
        Guid id, Guid facilityId, string autoclave, string? cycleNumber, string program, SterilizationCycleStatus status,
        string? @operator, decimal? temperatureC, decimal? pressureBar, int? durationMin,
        DateTimeOffset? startedOnUtc, DateTimeOffset? endedOnUtc, string? resultNotes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, autoclave, cycleNumber, program, status, @operator, temperatureC, pressureBar, durationMin,
            startedOnUtc, endedOnUtc, resultNotes, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Trim(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
