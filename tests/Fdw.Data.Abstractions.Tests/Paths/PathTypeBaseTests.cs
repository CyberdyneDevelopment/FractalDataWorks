using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Paths;

public sealed class PathTypeBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var pathType = new TestPathType(1, "SqlTable");

        // Assert
        pathType.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var pathType = new TestPathType(1, "SqlTable");

        // Assert
        pathType.Name.ShouldBe("SqlTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDomain()
    {
        // Arrange & Act
        var pathType = new TestPathType(1, "SqlTable");

        // Assert
        pathType.Domain.ShouldBe("Sql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsConfigurationKey()
    {
        // Arrange & Act
        var pathType = new TestPathType(1, "SqlTable");

        // Assert
        pathType.ConfigurationKey.ShouldBe("Paths:SqlTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDisplayName()
    {
        // Arrange & Act
        var pathType = new TestPathType(1, "SqlTable");

        // Assert
        pathType.DisplayName.ShouldBe("SQL Table Path");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDescription()
    {
        // Arrange & Act
        var pathType = new TestPathType(1, "SqlTable");

        // Assert
        pathType.Description.ShouldBe("Path to SQL table");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryDefaultsToPath()
    {
        // Arrange & Act
        var pathType = new TestPathType(1, "SqlTable");

        // Assert
        pathType.Category.ShouldBe("Path");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryCanBeCustomized()
    {
        // Arrange & Act
        var pathType = new TestPathType(2, "RestEndpoint", category: "API");

        // Assert
        pathType.Category.ShouldBe("API");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIPathType()
    {
        // Arrange
        var pathType = new TestPathType(1, "SqlTable");

        // Act & Assert
        pathType.ShouldBeAssignableTo<IPathType>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var pathType = new TestPathType(1, "SqlTable");

        // Act & Assert
        pathType.ShouldBeAssignableTo<PathTypeBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MultiplePathTypesForDifferentDomains()
    {
        // Arrange
        var sqlPath = new TestPathType(1, "SqlTable", domain: "Sql");
        var restPath = new TestPathType(2, "RestEndpoint", domain: "Rest");

        // Act & Assert
        sqlPath.Domain.ShouldBe("Sql");
        restPath.Domain.ShouldBe("Rest");
        sqlPath.Id.ShouldNotBe(restPath.Id);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPathType : PathTypeBase
    {
        public TestPathType(
            int id,
            string name,
            string? domain = null,
            string? category = null)
            : base(
                id,
                name,
                name == "SqlTable" ? "SQL Table Path" : "REST Endpoint Path",
                name == "SqlTable" ? "Path to SQL table" : "Path to REST endpoint",
                domain ?? "Sql",
                category)
        {
        }
    }
}
