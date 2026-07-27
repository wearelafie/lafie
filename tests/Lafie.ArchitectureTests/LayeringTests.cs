using System.Reflection;
using Lafie.Identity.Application;
using Lafie.Identity.Domain;
using Lafie.Interop.Application;
using Lafie.Interop.Domain;
using Lafie.Organization.Application;
using Lafie.Organization.Domain;
using Lafie.Patient.Application;
using Lafie.Patient.Domain;
using Lafie.Core;
using Lafie.Terminology.Application;
using Lafie.Terminology.Domain;
using NetArchTest.Rules;
using Xunit;

namespace Lafie.ArchitectureTests;

/// <summary>
/// Vérifie les frontières de l'architecture modulaire. Ces règles doivent rester
/// vertes à chaque phase : elles empêchent les dérives de couplage.
/// </summary>
public class LayeringTests
{
    // Préfixes des BuildingBlocks interdits au Domaine (il ne dépend que de Lafie.Core).
    // Point final pour ne pas confondre "Lafie.Shared" et... (aucun autre préfixe ambigu ici).
    private static readonly string[] FrameworkNs =
        ["Lafie.Shared.", "Lafie.Persistence.", "Lafie.Eventing.", "Lafie.Web."];

    private static readonly string[] ModuleNames =
        ["Identity", "Organization", "Patient", "Terminology", "Interop"];

    private static readonly Dictionary<string, Assembly[]> ModuleAssemblies = new()
    {
        ["Identity"] = [typeof(IdentityDomainMarker).Assembly, typeof(IdentityApplicationMarker).Assembly],
        ["Organization"] = [typeof(OrganizationDomainMarker).Assembly, typeof(OrganizationApplicationMarker).Assembly],
        ["Patient"] = [typeof(PatientDomainMarker).Assembly, typeof(PatientApplicationMarker).Assembly],
        ["Terminology"] = [typeof(TerminologyDomainMarker).Assembly, typeof(TerminologyApplicationMarker).Assembly],
        ["Interop"] = [typeof(InteropDomainMarker).Assembly, typeof(InteropApplicationMarker).Assembly],
    };

    private static readonly Assembly[] DomainAssemblies =
    [
        typeof(IdentityDomainMarker).Assembly,
        typeof(OrganizationDomainMarker).Assembly,
        typeof(PatientDomainMarker).Assembly,
        typeof(TerminologyDomainMarker).Assembly,
        typeof(InteropDomainMarker).Assembly,
    ];

    [Fact]
    public void Domain_ne_depend_ni_de_Shared_ni_d_Infrastructure()
    {
        foreach (var assembly in DomainAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(FrameworkNs)
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                $"{assembly.GetName().Name} viole l'isolation du domaine : "
                + string.Join(", ", result.FailingTypeNames ?? []));
        }
    }

    [Fact]
    public void Les_modules_ne_se_referencent_pas_entre_eux()
    {
        foreach (var module in ModuleNames)
        {
            var forbidden = ModuleNames
                .Where(other => other != module)
                .Select(other => $"Lafie.{other}.")
                .ToArray();

            foreach (var assembly in ModuleAssemblies[module])
            {
                var result = Types.InAssembly(assembly)
                    .ShouldNot()
                    .HaveDependencyOnAny(forbidden)
                    .GetResult();

                Assert.True(
                    result.IsSuccessful,
                    $"{assembly.GetName().Name} dépend d'un autre module : "
                    + string.Join(", ", result.FailingTypeNames ?? []));
            }
        }
    }

    [Fact]
    public void SharedKernel_ne_depend_de_rien_d_autre_dans_Lafie()
    {
        var result = Types.InAssembly(typeof(ILafieCore).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(FrameworkNs)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Lafie.Core ne doit dépendre d'aucun autre projet Lafie : "
            + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
