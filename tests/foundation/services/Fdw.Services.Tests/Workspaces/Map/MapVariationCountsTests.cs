using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces.Endpoints;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1029: a data set's variations are counted by kind and carried on the data set, not drawn as nodes.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapVariationCountsTests
{
    private static readonly Guid Orders = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Customers = Guid.Parse("00000000-0000-0000-0000-0000000000a2");

    private static DataSetVariationDomainConfiguration Variation(Guid baseDataSetId, string kind) => new()
    {
        Id = Guid.NewGuid(),
        BaseDataSetId = baseDataSetId,
        Implementation = kind,
    };

    [Fact]
    public void VariationsAreCountedPerKindForEachDataSetInKindOrder()
    {
        var counts = MapVariationCounts.Of(
        [
            Variation(Orders, "Scoped"),
            Variation(Orders, "DataView"),
            Variation(Orders, "Scoped"),
            Variation(Customers, "DataView"),
        ]);

        counts[Orders].Select(count => (count.Kind, count.Count)).ShouldBe([("DataView", 1), ("Scoped", 2)]);
        counts[Customers].Select(count => (count.Kind, count.Count)).ShouldBe([("DataView", 1)]);
    }

    [Fact]
    public void ADataSetWithNoVariationsHasNoEntry()
    {
        MapVariationCounts.Of([Variation(Orders, "Scoped")]).ContainsKey(Customers).ShouldBeFalse();
    }
}
