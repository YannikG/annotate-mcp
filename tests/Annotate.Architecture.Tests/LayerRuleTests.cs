using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;

namespace Annotate.Architecture.Tests;

public sealed class LayerRuleTests
{
    [Fact]
    public void DomainFixtureDependingOnEntityFrameworkFails()
    {
        _ = Fixtures.Domain.DomainFixture.Touch();
        IArchRule rule = LayerRules.DomainFreeOfEntityFramework();
        Assert.Throws<FailedArchRuleException>(() => ArchRuleAssert.CheckRule(LayerRules.FixtureArchitecture(), rule));
    }

    [Fact]
    public void ProductDomainDoesNotDependOnEntityFramework()
    {
        IArchRule rule = LayerRules.DomainFreeOfEntityFramework();
        ArchRuleAssert.CheckRule(LayerRules.ProductArchitecture(), rule);
    }

    [Fact]
    public void PublicInfrastructureFixtureFails()
    {
        IArchRule rule = LayerRules.InfrastructureTypesAreNotPublic();
        Assert.Throws<FailedArchRuleException>(() => ArchRuleAssert.CheckRule(LayerRules.FixtureArchitecture(), rule));
    }

    [Fact]
    public void ProductInfrastructureTypesAreNotPublic()
    {
        IArchRule rule = LayerRules.InfrastructureTypesAreNotPublic();
        ArchRuleAssert.CheckRule(LayerRules.ProductArchitecture(), rule);
    }

    [Fact]
    public void WebFixtureDependingOnInfrastructureFails()
    {
        _ = Fixtures.Web.WebInfrastructureFixture.Touch();
        IArchRule rule = LayerRules.AssemblyDoesNotDependOnInfrastructure(typeof(LayerRuleTests).Assembly);
        Assert.Throws<FailedArchRuleException>(() => ArchRuleAssert.CheckRule(LayerRules.FixtureArchitecture(), rule));
    }

    [Fact]
    public void ProductWebDoesNotDependOnInfrastructure()
    {
        System.Reflection.Assembly web = System.Reflection.Assembly.Load("Annotate.Web");
        ArchUnitNET.Domain.Architecture architecture = LayerRules.ProductArchitecture();
        Assert.Contains(architecture.Types, type => type.Assembly.Name == "Annotate.Web");
        IArchRule rule = LayerRules.AssemblyDoesNotDependOnInfrastructure(web);
        ArchRuleAssert.CheckRule(architecture, rule);
    }
}