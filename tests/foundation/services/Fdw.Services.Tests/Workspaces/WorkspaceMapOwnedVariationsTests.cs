using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>FDW-3638: a variation a workspace owns is a data set it owns, so its map draws it with an edge from what it varies.</summary>
[Collection(nameof(ServicesTestCollection))]
public class WorkspaceMapOwnedVariationsTests
{
    private static readonly Guid Base = Guid.Parse("00000000-0000-0000-0000-0000000000b1");

    private sealed class Probe : GetWorkspaceMapEndpointBase
    {
        public Probe()
            : base(
                NullLogger<GetWorkspaceMapEndpointBase>.Instance,
                Mock.Of<IWorkspaceConfigurationProvider>(),
                Mock.Of<IWorkspaceAccessPolicy>(),
                Mock.Of<IDataSetVariationConfigurationProvider>())
        {
        }

        public static WorkspaceMapResponse Map(IWorkspaceImplementationConfiguration workspace, IReadOnlyList<DataSetVariationDomainConfiguration> owned)
            => ToMap(workspace, MapVariationCounts.Of(owned), owned);
    }

    private static IWorkspaceImplementationConfiguration Workspace()
    {
        var workspace = new Mock<IWorkspaceImplementationConfiguration>();
        workspace.SetupGet(w => w.Resources).Returns(new List<WorkspaceResourceConfiguration>());
        workspace.SetupGet(w => w.Relationships).Returns(new List<WorkspaceRelationshipConfiguration>());
        return workspace.Object;
    }

    private static DataSetVariationDomainConfiguration Variation(Guid? parent = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "AbacusSolutions/GasPrices",
        VariationName = "GasPrices",
        VariationKind = VariationKinds.Scoped,
        OwnerWorkspaceId = Guid.NewGuid(),
        BaseDataSetId = Base,
        ParentVariationId = parent,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AnOwnedVariationIsANodeWithAnEdgeFromTheDataSetItVaries()
    {
        var variation = Variation();

        var map = Probe.Map(Workspace(), [variation]);

        var node = map.Nodes.ShouldHaveSingleItem();
        node.Id.ShouldBe(variation.Id.ToString());
        node.Label.ShouldBe("GasPrices");
        node.NodeType.ShouldBe("DataSet");
        node.Category.ShouldBe("Owns");
        var edge = map.Edges.ShouldHaveSingleItem();
        edge.Source.ShouldBe(Base.ToString());
        edge.Target.ShouldBe(variation.Id.ToString());
        edge.IsDefined.ShouldBeTrue();
        edge.RelationType.ShouldBe(VariationKinds.Scoped);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AViewOfAViewHasItsEdgeFromTheViewItNarrows()
    {
        var parent = Guid.NewGuid();

        var map = Probe.Map(Workspace(), [Variation(parent)]);

        map.Edges.Single().Source.ShouldBe(parent.ToString());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AWorkspaceThatOwnsNoVariationDrawsNone()
    {
        var map = Probe.Map(Workspace(), []);

        map.Nodes.ShouldBeEmpty();
        map.Edges.ShouldBeEmpty();
    }
}
