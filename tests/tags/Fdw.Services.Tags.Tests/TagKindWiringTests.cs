using System;
using System.Linq;
using Fdw.Services.Tags.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tags.Tests;

/// <summary>Every taggable kind must have a link store of the same name, and the kinds that sit in a workspace must name a known workspace resource kind.</summary>
[Trait("Category", "Tags")]
public sealed class TagKindWiringTests
{
    [Fact]
    [Trait("Priority", "P0")]
    public void EveryTaggableKindHasALinkStoreOfTheSameName()
    {
        foreach (var kind in TaggableKinds.All())
        {
            ReferenceEquals(TagLinkStores.ByName(kind.Name), TagLinkStores.NotFound).ShouldBeFalse($"kind '{kind.Name}' has no link store");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void EveryTaggableKindNamesItsLinkTableAndThingColumn()
    {
        foreach (var kind in TaggableKinds.All())
        {
            kind.PathName.ShouldNotBeNullOrWhiteSpace();
            kind.ContainerName.ShouldEndWith("Tag");
            kind.ThingIdColumn.ShouldBe(kind.Name + "Id");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void TheShippedKindsAreTheSixTheLinkTablesCover()
    {
        TaggableKinds.All().Select(kind => kind.Name).Order().ToArray().ShouldBe(["Calculation", "Connection", "DataSet", "DataStore", "Pipeline", "Schedule"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ConnectionAndScheduleAreTenantWideAndTheOthersSitInAWorkspace()
    {
        TaggableKinds.ByName("Connection").WorkspaceResourceKind.ShouldBe(string.Empty);
        TaggableKinds.ByName("Schedule").WorkspaceResourceKind.ShouldBe(string.Empty);
        TaggableKinds.ByName("DataSet").WorkspaceResourceKind.ShouldBe("DataSet");
        TaggableKinds.ByName("Pipeline").WorkspaceResourceKind.ShouldBe("Pipeline");
    }
}
