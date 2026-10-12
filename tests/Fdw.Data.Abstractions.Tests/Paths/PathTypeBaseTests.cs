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
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var pathType = new TestPathType("SqlTable");

        // Assert
        pathType.Name.ShouldBe("SqlTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDomain()
    {
        // Arrange & Act
        var pathType = new TestPathType("SqlTable");

        // Assert
        pathType.Domain.ShouldBe("Sql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsConfigurationKey()
    {
        // Arrange & Act
        var pathType = new TestPathType("SqlTable");

        // Assert
        pathType.ConfigurationKey.ShouldBe("Paths:SqlTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDisplayName()
    {
        // Arrange & Act
        var pathType = new TestPathType("SqlTable");

        // Assert
        pathType.DisplayName.ShouldBe("SQL Table Path");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDescription()
    {
        // Arrange & Act
        var pathType = new TestPathType("SqlTable");

        // Assert
        pathType.Description.ShouldBe("Path to SQL table");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryDefaultsToPath()
    {
        // Arrange & Act
        var pathType = new TestPathType("SqlTable");

        // Assert
        pathType.Category.ShouldBe("Path");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryCanBeCustomized()
    {
        // Arrange & Act
        var pathType = new TestPathType("RestEndpoint", category: "API");

        // Assert
        pathType.Category.ShouldBe("API");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIPathType()
    {
        // Arrange
        var pathType = new TestPathType("SqlTable");

        // Act & Assert
        pathType.ShouldBeAssignableTo<IPathType>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var pathType = new TestPathType("SqlTable");

        // Act & Assert
        pathType.ShouldBeAssignableTo<PathTypeBase>();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPathType : PathTypeBase
    {
        public TestPathType(
            string name,
            string? domain = null,
            string? category = null)
            : base(
                name,
                name == "SqlTable" ? "SQL Table Path" : "REST Endpoint Path",
                name == "SqlTable" ? "Path to SQL table" : "Path to REST endpoint",
                domain ?? "Sql",
                category)
        {
        }
    }
}
