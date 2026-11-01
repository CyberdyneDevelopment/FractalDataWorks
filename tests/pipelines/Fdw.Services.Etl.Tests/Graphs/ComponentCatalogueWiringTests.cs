using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Graphs.Describing;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests that the component catalogue and the code that runs and describes it agree (FDW-1396, FDW-1397): a kind the catalogue
/// lists with nodes can be built into a flow component, and a kind with output nodes can have its fields derived, so a new kind
/// cannot be added half-wired.
/// </summary>
public sealed class ComponentCatalogueWiringTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EveryKindWithNodesHasABuilder()
    {
        var unbuilt = ComponentTypes.All()
            .Where(kind => kind.Inputs.Count > 0 || kind.Outputs.Count > 0)
            .Where(kind => ReferenceEquals(FlowComponentBuilders.ByName(kind.Name), FlowComponentBuilders.NotFound))
            .Select(kind => kind.Name)
            .ToList();

        unbuilt.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EveryKindWithOutputNodesHasAFieldDeriver()
    {
        var underived = ComponentTypes.All()
            .Where(kind => kind.Outputs.Count > 0)
            .Where(kind => ReferenceEquals(ComponentFieldDerivers.ByName(kind.Name), ComponentFieldDerivers.NotFound))
            .Select(kind => kind.Name)
            .ToList();

        underived.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NoBuilderOrDeriverNamesAKindTheCatalogueLacks()
    {
        var kinds = ComponentTypes.All().Select(kind => kind.Name).ToHashSet(System.StringComparer.Ordinal);

        FlowComponentBuilders.All().Select(b => b.Name).Where(name => !kinds.Contains(name)).ShouldBeEmpty();
        ComponentFieldDerivers.All().Select(d => d.Name).Where(name => !kinds.Contains(name)).ShouldBeEmpty();
    }
}
