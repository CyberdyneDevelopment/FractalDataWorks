using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Builders;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// A store built as a slice of the containers a command names (FDW-4328).
/// </summary>
/// <remarks>
/// Why the whole build beside it: the slice must give a named container the keys the whole build gives it, and differ only in what it leaves out. Two
/// containers point at each other (A and B, as T001 and T002 do in the 500-container rig), so a cycle is what is built.
/// </remarks>
public sealed class DataStoreSliceBuildTests
{
    private static readonly Guid AIdField = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a");
    private static readonly Guid BIdField = Guid.Parse("bbbbbbbb-0000-0000-0000-00000000000b");

    private static DataStoreImplementationConfiguration Store() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Rig",
        Paths =
        [
            new DataPathConfiguration
            {
                Id = Guid.NewGuid(),
                Name = "dsperf",
                Containers = [Container("A", AIdField, "B"), Container("B", BIdField, "A")],
            },
        ],
    };

    private static DataContainerConfiguration Container(string name, Guid idField, string pointsAt) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Fields =
        [
            new DataContainerFieldConfiguration { Id = idField, Name = "Id", Ordinal = 0 },
            new DataContainerFieldConfiguration { Id = Guid.NewGuid(), Name = "RefId", Ordinal = 1 },
        ],
        Keys =
        [
            new DataContainerKeyConfiguration
            {
                Id = Guid.NewGuid(), Name = $"Surrogate_{name}", TypeId = "Surrogate",
                KeyFields = [new DataContainerKeyFieldConfiguration { Id = Guid.NewGuid(), DataContainerFieldId = idField, Ordinal = 0 }],
            },
            new DataContainerKeyConfiguration
            {
                Id = Guid.NewGuid(), Name = $"FK_{name}_Ref", TypeId = "Foreign",
                ReferencedContainerName = pointsAt, ReferencedKeyName = $"Surrogate_{pointsAt}",
                KeyFields = [new DataContainerKeyFieldConfiguration { Id = Guid.NewGuid(), Name = "RefId", Ordinal = 0 }],
            },
        ],
    };

    private static async Task<IDataStore> Build(IReadOnlyCollection<string>? slice)
    {
        var builder = new GenericDataStoreBuilder(FormatTypes.NotFound);
        builder.Configure(Store()).IsSuccess.ShouldBeTrue();
        if (slice is not null)
            builder.DeclareSlice(slice);

        var built = await builder.Build(TestContext.Current.CancellationToken);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        return built.Value!;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASliceListsTheContainersItNamesAndNoOther()
    {
        var store = await Build(["A"]);

        store.Paths.Single().Containers.Select(c => c.Name).ShouldBe(["A"]);
        store.Paths.Single().Container("B").IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANamedContainerHasTheKeysAndReferencedContainerTheWholeBuildGivesIt()
    {
        var whole = (await Build(null)).Paths.Single().Container("A").Value!;
        var slice = (await Build(["A"])).Paths.Single().Container("A").Value!;

        slice.Keys.Select(k => k.KeyName).ShouldBe(whole.Keys.Select(k => k.KeyName));
        var sliceForeign = slice.Keys.Single(k => k.ReferencedContainer is not null);
        var wholeForeign = whole.Keys.Single(k => k.ReferencedContainer is not null);
        sliceForeign.ReferencedContainer!.Name.ShouldBe(wholeForeign.ReferencedContainer!.Name);
        sliceForeign.KeyFields.Single().ReferencedField!.Name.ShouldBe(wholeForeign.KeyFields.Single().ReferencedField!.Name);
        slice.Nodes.Select(n => n.Name).ShouldBe(whole.Nodes.Select(n => n.Name));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AContainerOnlyReferencedIsTheBareNodeSoACycleStillResolves()
    {
        var slice = (await Build(["A"])).Paths.Single().Container("A").Value!;

        var referenced = slice.Keys.Single(k => k.ReferencedContainer is not null).ReferencedContainer!;

        referenced.Name.ShouldBe("B");
        referenced.Keys.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheKeysPointingAtANamedContainerAreNotResolvedAndSaySo()
    {
        var slice = (await Build(["A"])).Paths.Single().Container("A").Value!;

        slice.ReferencingKeys.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AWholeBuildStillResolvesTheKeysPointingAtAContainer()
    {
        var whole = (await Build(null)).Paths.Single().Container("A").Value!;

        whole.ReferencingKeys.IsSuccess.ShouldBeTrue();
        whole.ReferencingKeys.Value!.Select(b => b.Owner.Name).ShouldBe(["B"]);
    }
}
