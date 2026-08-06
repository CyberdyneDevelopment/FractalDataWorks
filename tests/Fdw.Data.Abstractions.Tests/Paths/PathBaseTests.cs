using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Paths;

public sealed class PathBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var path = new TestPath(1, "TestPath");

        // Assert
        path.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var path = new TestPath(1, "TestPath");

        // Assert
        path.Name.ShouldBe("TestPath");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryIsSetToPath()
    {
        // Arrange & Act
        var path = new TestPath(1, "TestPath");

        // Assert
        path.Category.ShouldBe("Path");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PathValueReturnsImplementationValue()
    {
        // Arrange & Act
        var path = new TestPath(1, "SqlPath");

        // Assert
        path.PathValue.ShouldBe("/test/path/value");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DomainReturnsImplementationValue()
    {
        // Arrange & Act
        var path = new TestPath(1, "SqlPath");

        // Assert
        path.Domain.ShouldBe("Sql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIPath()
    {
        // Arrange
        var path = new TestPath(1, "TestPath");

        // Act & Assert
        path.ShouldBeAssignableTo<IPath>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var path = new TestPath(1, "TestPath");

        // Act & Assert
        path.ShouldBeAssignableTo<PathBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MultipleInstancesWithDifferentIds()
    {
        // Arrange
        var path1 = new TestPath(1, "Path1");
        var path2 = new TestPath(2, "Path2");

        // Act & Assert
        path1.Id.ShouldNotBe(path2.Id);
        path1.Name.ShouldNotBe(path2.Name);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPath : PathBase
    {
        public TestPath(int id, string name) : base(id, name)
        {
        }

        public override string PathValue => "/test/path/value";
        public override string Domain => "Sql";
    }
}
