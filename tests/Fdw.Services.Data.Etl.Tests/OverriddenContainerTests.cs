using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Etl.Tests;

/// <summary>
/// FDW-1952, FDW-1967: the ETL data gateway reads a declared container at another physical path and/or name. The declared container supplies
/// the shape; the connection is given the path the declared path's own kind moved (a table is read from the path), and a name that is not
/// stated keeps the declared name.
/// </summary>
public sealed class OverriddenContainerTests
{
    private static Mock<IDataContainer> Declared(string name, string pathValue)
    {
        var path = new Mock<IPath>();
        path.SetupGet(p => p.PathValue).Returns(pathValue);
        path.SetupGet(p => p.Domain).Returns("Files");

        var declared = new Mock<IDataContainer>();
        declared.SetupGet(c => c.Name).Returns(name);
        declared.SetupGet(c => c.Path).Returns(path.Object);
        declared.SetupGet(c => c.SupportedOperations).Returns(["Query"]);
        declared.SetupGet(c => c.Metadata).Returns(new Dictionary<string, object> { ["owner"] = "etl" });
        return declared;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStatedNameReplacesTheDeclaredName()
    {
        var declared = Declared("Orders", "inbound").Object;

        var container = new OverriddenContainer(declared, declared.Path, "orders-2026-10-19.csv");

        container.Name.ShouldBe("orders-2026-10-19.csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheConnectionIsGivenThePathItWasMovedTo()
    {
        var moved = new Mock<IPath>();
        moved.SetupGet(p => p.PathValue).Returns("inbound/ercot");

        var container = new OverriddenContainer(Declared("Orders", "inbound").Object, moved.Object, null);

        container.Path.ShouldBeSameAs(moved.Object);
        container.Name.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ABlankNameKeepsTheDeclaredName()
    {
        var declared = Declared("Orders", "inbound").Object;

        new OverriddenContainer(declared, declared.Path, " ").Name.ShouldBe("Orders");
        new OverriddenContainer(declared, declared.Path, null).Name.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheShapeIsTheDeclaredContainers()
    {
        var declared = Declared("Orders", "inbound");

        var container = new OverriddenContainer(declared.Object, declared.Object.Path, "other.csv");

        container.SupportedOperations.ShouldBe(["Query"]);
        container.Metadata["owner"].ShouldBe("etl");
        container.Description.ShouldBe(declared.Object.Description);
    }
}
