using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// FDW-5287: the families of component kind a Graph data set refuses (<see cref="GraphRefusedComponentKinds"/>, in the data sets package, which cannot see the
/// component catalogue) agree with the catalogue: a new destination, action or loop kind that is not listed there fails this test, so it is never stored in a graph.
/// </summary>
public sealed class GraphRefusedComponentKindsTests
{
    private static string FamilyOf(IComponentType kind) =>
        kind.HoldsComponents ? "Loop"
        : kind.IsAction ? "Action"
        : string.Equals(kind.Category, "Destination", StringComparison.Ordinal) ? "Destination"
        : string.Empty;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EveryDestinationActionAndLoopKindOfTheCatalogueIsInItsRefusedFamily()
    {
        foreach (var kind in ComponentTypes.All().Where(kind => FamilyOf(kind).Length > 0))
        {
            var family = GraphRefusedComponentKinds.ByName(FamilyOf(kind));

            family.ComponentTypeNames.ShouldContain(kind.Name, $"component kind '{kind.Name}' is a {FamilyOf(kind)} and must be refused by a Graph data set");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EveryRefusedNameIsAKindOfTheCatalogueOfTheFamilyItIsListedIn()
    {
        foreach (var family in GraphRefusedComponentKinds.All())
        {
            foreach (var name in family.ComponentTypeNames)
            {
                var kind = ComponentTypes.ByName(name);

                ReferenceEquals(kind, ComponentTypes.NotFound).ShouldBeFalse($"'{name}' is not a component kind");
                FamilyOf(kind).ShouldBe(family.Name);
            }
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void NoSourceJoinOrToolKindIsRefused()
    {
        var refused = GraphRefusedComponentKinds.All().SelectMany(family => family.ComponentTypeNames).ToList();

        foreach (var kind in ComponentTypes.All().Where(kind => FamilyOf(kind).Length == 0))
        {
            refused.ShouldNotContain(kind.Name);
        }
    }
}
