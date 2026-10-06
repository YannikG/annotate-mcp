using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Annotate.Architecture.Tests;

internal static class LayerRules
{
    public static ArchUnitNET.Domain.Architecture FixtureArchitecture() =>
        new ArchLoader().LoadAssemblies(typeof(LayerRules).Assembly).Build();

    public static ArchUnitNET.Domain.Architecture ProductArchitecture() =>
        new ArchLoader()
            .LoadAssemblies(
                System.Reflection.Assembly.Load("Annotate.Markdown"),
                System.Reflection.Assembly.Load("Annotate.Plans"),
                System.Reflection.Assembly.Load("Annotate.Reviews"),
                System.Reflection.Assembly.Load("Annotate.Web"))
            .Build();

    public static IArchRule DomainFreeOfEntityFramework() =>
        Types()
            .That()
            .ResideInNamespaceMatching(@"\.Domain$")
            .Should()
            .NotDependOnAnyTypesThat()
            .ResideInNamespaceMatching(@"Microsoft\.EntityFrameworkCore")
            .WithoutRequiringPositiveResults();

    public static IArchRule InfrastructureTypesAreNotPublic() =>
        Types()
            .That()
            .ResideInNamespaceMatching(@"\.Infrastructure")
            .Should()
            .NotBePublic()
            .WithoutRequiringPositiveResults();

    public static IArchRule AssemblyDoesNotDependOnInfrastructure(System.Reflection.Assembly assembly) =>
        Types()
            .That()
            .ResideInAssembly(assembly.FullName!)
            .Should()
            .NotDependOnAnyTypesThat()
            .ResideInNamespaceMatching(@"\.Infrastructure")
            .WithoutRequiringPositiveResults();
}