using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections;
using Fdw.Services.Workspaces.Endpoints;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Lineage;

/// <summary>FDW-3824: a pipeline names a container by data store, path and name; it is found in exactly one place or not at all.</summary>
public class LineageContainerEndsTests
{
    private static IDataStoreImplementationConfiguration Store(params (string Path, string[] Containers)[] paths)
    {
        var store = new Mock<IDataStoreImplementationConfiguration>();
        store.SetupGet(s => s.Paths).Returns(paths
            .Select(p => new DataPathConfiguration
            {
                Name = p.Path,
                Containers = p.Containers.Select(name => new DataContainerConfiguration { Name = name }).ToList(),
            })
            .ToList());
        return store.Object;
    }

    [Fact]
    public void ANamedPathNarrowsTheSearchToThatPath()
    {
        var store = Store(("landing", ["Raw"]), ("archive", ["Raw"]));

        var found = LineageContainerEnds.Find(store, new PipelineContainerRef("Lake", "archive", "Raw"));

        found.ShouldNotBeNull().Path.Name.ShouldBe("archive");
    }

    [Fact]
    public void WithNoPathNamedAContainerInOnePathIsFound()
    {
        var store = Store(("landing", ["Raw"]), ("archive", ["Old"]));

        LineageContainerEnds.Find(store, new PipelineContainerRef("Lake", null, "raw")).ShouldNotBeNull().Path.Name.ShouldBe("landing");
    }

    [Fact]
    public void WithNoPathNamedAContainerInTwoPathsIsNotGuessed()
    {
        var store = Store(("landing", ["Raw"]), ("archive", ["Raw"]));

        LineageContainerEnds.Find(store, new PipelineContainerRef("Lake", null, "Raw")).ShouldBeNull();
    }

    [Fact]
    public void AContainerTheStoreDoesNotHoldIsNotFound()
    {
        LineageContainerEnds.Find(Store(("landing", ["Raw"])), new PipelineContainerRef("Lake", "landing", "Missing")).ShouldBeNull();
    }
}
