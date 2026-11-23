using Lafie.Organization.Application.Sterilization.Cycles;
using Lafie.Organization.Application.Waste.Slips;
using Lafie.Organization.Domain;
using Lafie.Organization.Infrastructure;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.IntegrationTests;

/// <summary>
/// Garde multi-tenant « cross-entité » : une opération qui rattache deux ressources (boîte↔cycle,
/// collecte↔bordereau) doit refuser que l'enfant appartienne à un autre établissement que le parent.
/// Complète la garde tenant posée côté endpoint (<c>RequireFacilityScope</c>) : ici on prouve la
/// seconde couche (même établissement) directement dans les handlers, avec les vrais repositories.
/// </summary>
public class CrossFacilityGuardTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private async Task<Guid> NewFacilityAsync()
    {
        var facility = DomainOrganization.Create($"F{Guid.NewGuid():N}", "Facility", "HOSPITAL", "PUBLIC", "TG").Value;
        await new OrganizationRepository(fixture.ConnectionFactory).AddAsync(facility);
        return facility.Id;
    }

    // ---------------------------------------------------------------- Stérilisation : boîte ↔ cycle

    [Fact]
    public async Task AddSetToCycle_rejects_set_from_another_facility()
    {
        var cycles = new SterilizationCycleRepository(fixture.ConnectionFactory);
        var sets = new InstrumentSetRepository(fixture.ConnectionFactory);
        var facilityA = await NewFacilityAsync();
        var facilityB = await NewFacilityAsync();

        var cycle = SterilizationCycle.Create(facilityA, "Autoclave-1", "C-1", "134°C", "op").Value; // Planned
        await cycles.AddAsync(cycle);
        var foreignSet = InstrumentSet.Create(facilityB, "BOITE-B", "Boîte de B", null, null, 5).Value; // autre établissement
        await sets.AddAsync(foreignSet);

        var result = await new AddSetToCycleHandler(cycles, sets)
            .Handle(new AddSetToCycleCommand(cycle.Id, foreignSet.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        // La boîte de B ne doit pas être entrée en charge, ni avoir changé d'état.
        Assert.False(await cycles.IsInLoadAsync(cycle.Id, foreignSet.Id));
        var reloaded = await sets.FindAsync(foreignSet.Id);
        Assert.Equal(InstrumentSetStatus.Soiled, reloaded!.Status);
    }

    [Fact]
    public async Task AddSetToCycle_accepts_set_from_same_facility()
    {
        var cycles = new SterilizationCycleRepository(fixture.ConnectionFactory);
        var sets = new InstrumentSetRepository(fixture.ConnectionFactory);
        var facilityA = await NewFacilityAsync();

        var cycle = SterilizationCycle.Create(facilityA, "Autoclave-1", "C-2", "134°C", "op").Value;
        await cycles.AddAsync(cycle);
        var set = InstrumentSet.Create(facilityA, "BOITE-A", "Boîte de A", null, null, 5).Value;
        await sets.AddAsync(set);

        var result = await new AddSetToCycleHandler(cycles, sets)
            .Handle(new AddSetToCycleCommand(cycle.Id, set.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(await cycles.IsInLoadAsync(cycle.Id, set.Id));
        var reloaded = await sets.FindAsync(set.Id);
        Assert.Equal(InstrumentSetStatus.InSterilization, reloaded!.Status);
    }

    // ------------------------------------------------------------------ Déchets : collecte ↔ bordereau

    [Fact]
    public async Task AddCollectionToSlip_rejects_collection_from_another_facility()
    {
        var slips = new WasteSlipRepository(fixture.ConnectionFactory);
        var collections = new WasteCollectionRepository(fixture.ConnectionFactory);
        var facilityA = await NewFacilityAsync();
        var facilityB = await NewFacilityAsync();

        var slip = WasteEliminationSlip.Create(facilityA, "BORD-A", WasteStream.Dasri, "Transporteur", "Site", WasteTreatment.Incineration).Value; // Draft
        await slips.AddAsync(slip);
        // Collecte d'un AUTRE établissement, mais MÊME filière → seul le facility doit la faire échouer.
        var foreignCollection = WasteCollection.Create(facilityB, WasteStream.Dasri, 12.5m, 3, null, null, null, null).Value;
        await collections.AddAsync(foreignCollection);

        var result = await new AddCollectionToSlipHandler(slips, collections)
            .Handle(new AddCollectionToSlipCommand(slip.Id, foreignCollection.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        var reloaded = await collections.FindAsync(foreignCollection.Id);
        Assert.Equal(WasteCollectionStatus.Collected, reloaded!.Status); // non rattachée
        Assert.Null(reloaded.SlipId);
    }

    [Fact]
    public async Task AddCollectionToSlip_accepts_collection_from_same_facility()
    {
        var slips = new WasteSlipRepository(fixture.ConnectionFactory);
        var collections = new WasteCollectionRepository(fixture.ConnectionFactory);
        var facilityA = await NewFacilityAsync();

        var slip = WasteEliminationSlip.Create(facilityA, "BORD-A2", WasteStream.Dasri, "Transporteur", "Site", WasteTreatment.Incineration).Value;
        await slips.AddAsync(slip);
        var collection = WasteCollection.Create(facilityA, WasteStream.Dasri, 12.5m, 3, null, null, null, null).Value;
        await collections.AddAsync(collection);

        var result = await new AddCollectionToSlipHandler(slips, collections)
            .Handle(new AddCollectionToSlipCommand(slip.Id, collection.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await collections.FindAsync(collection.Id);
        Assert.Equal(slip.Id, reloaded!.SlipId);
    }
}
