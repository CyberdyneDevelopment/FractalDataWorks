using System.Linq;
using Shouldly;
using Xunit;

namespace Fdw.Schema.Tests;

public class DataLayoutsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsAllFiveLayouts()
    {
        // Act
        var layouts = DataLayouts.All();

        // Assert
        layouts.ShouldNotBeNull();
        layouts.Count().ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TabularLayoutExists()
    {
        // Act
        var tabular = DataLayouts.ByName("Tabular");

        // Assert
        tabular.ShouldNotBeNull();
        tabular.Name.ShouldBe("Tabular");
        tabular.IsTabular.ShouldBeTrue();
        tabular.SupportsNesting.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HierarchicalLayoutExists()
    {
        // Act
        var hierarchical = DataLayouts.ByName("Hierarchical");

        // Assert
        hierarchical.ShouldNotBeNull();
        hierarchical.Name.ShouldBe("Hierarchical");
        hierarchical.SupportsNesting.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DocumentLayoutExists()
    {
        // Act
        var document = DataLayouts.ByName("Document");

        // Assert
        document.ShouldNotBeNull();
        document.Name.ShouldBe("Document");
        document.SupportsNesting.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void KeyValueLayoutExists()
    {
        // Act
        var keyValue = DataLayouts.ByName("KeyValue");

        // Assert
        keyValue.ShouldNotBeNull();
        keyValue.Name.ShouldBe("KeyValue");
        keyValue.SupportsNesting.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GraphLayoutExists()
    {
        // Act
        var graph = DataLayouts.ByName("Graph");

        // Assert
        graph.ShouldNotBeNull();
        graph.Name.ShouldBe("Graph");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectLayout()
    {
        // Arrange
        var tabularByName = DataLayouts.ByName("Tabular");

        // Act
        var tabularById = DataLayouts.ById(tabularByName.Id);

        // Assert
        tabularById.ShouldBe(tabularByName);
    }

}
