using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Data.MsSql;
using Moq;
// Why: Phase 1 introduced IDataNodePath in Data.Abstractions alongside the pre-existing one in
// DataStores.Abstractions. This file predates Phase 1 and uses the old interface throughout.
using IDataNodePath = Fdw.Data.DataStores.Abstractions.IDataPath;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Paths;

/// <summary>
/// Gap tests for DatabasePath - covers container operations with actual items,
/// FullPath, Parameters, Metadata properties, and additional IDataNodePath members.
/// </summary>
public sealed class DatabasePathGapTests
{
    // Why (foundational redesign): the old ContainerBase-derived TableContainer was deleted. These
    // tests only need an IStorageContainer with a Name to exercise DatabasePath container navigation,
    // so a lightweight mock stands in for the concrete container type.
    private static IStorageContainer MakeContainer(string name)
    {
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        return container.Object;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithContainersStoresContainerList()
    {
        // Arrange
        var dbPath = new DatabasePath("", "dbo", "test");
        var container1 = MakeContainer("Table1");

        // Act
        var sut = new DatabasePath("db", "dbo", "Table1", new[] { (IStorageContainer)container1 });

        // Assert
        sut.Containers.Count.ShouldBe(1);
        sut.Containers[0].Name.ShouldBe("Table1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetContainerFindsExistingContainerByName()
    {
        // Arrange
        var innerPath = new DatabasePath("", "dbo", "test");
        var container1 = MakeContainer("Customers");
        var container2 = MakeContainer("Orders");

        var sut = new DatabasePath("db", "dbo", "schema",
            new IStorageContainer[] { container1, container2 });

        // Act
        var found = sut.GetContainer("Customers");

        // Assert
        found.ShouldNotBeNull();
        found!.Name.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetContainerIsCaseInsensitive()
    {
        // Arrange
        var innerPath = new DatabasePath("", "dbo", "test");
        var container = MakeContainer("Customers");

        var sut = new DatabasePath("db", "dbo", "schema",
            new IStorageContainer[] { container });

        // Act
        var found = sut.GetContainer("customers");

        // Assert
        found.ShouldNotBeNull();
        found!.Name.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetContainerReturnsNullForWhitespace()
    {
        // Arrange
        var sut = new DatabasePath("db", "dbo", "table");

        // Act
        var found = sut.GetContainer("   ");

        // Assert
        found.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetContainerReturnsNullWhenNotFound()
    {
        // Arrange
        var innerPath = new DatabasePath("", "dbo", "test");
        var container = MakeContainer("Customers");

        var sut = new DatabasePath("db", "dbo", "schema",
            new IStorageContainer[] { container });

        // Act
        var found = sut.GetContainer("NonExistent");

        // Assert
        found.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsContainerReturnsTrueWhenPresent()
    {
        // Arrange
        var innerPath = new DatabasePath("", "dbo", "test");
        var container = MakeContainer("Customers");

        var sut = new DatabasePath("db", "dbo", "schema",
            new IStorageContainer[] { container });

        // Act & Assert
        sut.ContainsContainer("Customers").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsContainerIsCaseInsensitive()
    {
        // Arrange
        var innerPath = new DatabasePath("", "dbo", "test");
        var container = MakeContainer("Customers");

        var sut = new DatabasePath("db", "dbo", "schema",
            new IStorageContainer[] { container });

        // Act & Assert
        sut.ContainsContainer("CUSTOMERS").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsContainerReturnsFalseForWhitespace()
    {
        // Arrange
        var sut = new DatabasePath("db", "dbo", "table");

        // Act & Assert
        sut.ContainsContainer("   ").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsContainerReturnsFalseForNull()
    {
        // Arrange
        var sut = new DatabasePath("db", "dbo", "table");

        // Act & Assert
        sut.ContainsContainer(null!).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathFullPathIsSameAsPathValue()
    {
        // Arrange
        var sut = new DatabasePath("Northwind", "dbo", "Customers");

        // Act & Assert
        ((IDataNodePath)sut).FullPath.ShouldBe("Northwind.dbo.Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathFullPathWithoutDatabase()
    {
        // Arrange
        var sut = new DatabasePath("", "dbo", "Customers");

        // Act & Assert
        ((IDataNodePath)sut).FullPath.ShouldBe("dbo.Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathParametersReturnsEmptyDictionary()
    {
        // Arrange
        var sut = new DatabasePath("db", "dbo", "table");

        // Act
        var parameters = ((IDataNodePath)sut).Parameters;

        // Assert
        parameters.ShouldNotBeNull();
        parameters.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathMetadataReturnsEmptyDictionary()
    {
        // Arrange
        var sut = new DatabasePath("db", "dbo", "table");

        // Act
        var metadata = ((IDataNodePath)sut).Metadata;

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathIdWithEmptyDatabaseIncludesDot()
    {
        // Arrange
        var sut = new DatabasePath("", "dbo", "Customers");

        // Act & Assert
        ((IDataNodePath)sut).Id.ShouldBe(".dbo.Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullContainersUsesEmptyList()
    {
        // Arrange & Act
        var sut = new DatabasePath("db", "dbo", "table", null);

        // Assert
        sut.Containers.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TwoPartConstructorSetsCorrectProperties()
    {
        // Arrange & Act
        var sut = new DatabasePath("MyDb", "Products");

        // Assert
        sut.Database.ShouldBe("MyDb");
        sut.Schema.ShouldBe("dbo");
        sut.ObjectName.ShouldBe("Products");
        sut.PathValue.ShouldBe("MyDb.dbo.Products");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NullDatabaseSetToEmptyString()
    {
        // Arrange & Act
        var sut = new DatabasePath(null, "dbo", "table");

        // Assert
        sut.Database.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathSegmentsWithEmptyDatabaseStillHasThreeSegments()
    {
        // Arrange
        var sut = new DatabasePath("", "dbo", "Customers");

        // Act
        var segments = ((IDataNodePath)sut).Segments;

        // Assert
        segments.Count.ShouldBe(3);
        segments[0].ShouldBe("");
        segments[1].ShouldBe("dbo");
        segments[2].ShouldBe("Customers");
    }
}
