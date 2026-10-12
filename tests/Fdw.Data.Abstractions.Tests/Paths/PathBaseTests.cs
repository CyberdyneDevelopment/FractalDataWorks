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
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var path = new TestPath("TestPath");

        // Assert
        path.Name.ShouldBe("TestPath");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryIsSetToPath()
    {
        // Arrange & Act
        var path = new TestPath("TestPath");

        // Assert
        path.Category.ShouldBe("Path");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PathValueReturnsImplementationValue()
    {
        // Arrange & Act
        var path = new TestPath("SqlPath");

        // Assert
        path.PathValue.ShouldBe("/test/path/value");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DomainReturnsImplementationValue()
    {
        // Arrange & Act
        var path = new TestPath("SqlPath");

        // Assert
        path.Domain.ShouldBe("Sql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIPath()
    {
        // Arrange
        var path = new TestPath("TestPath");

        // Act & Assert
        path.ShouldBeAssignableTo<IPath>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var path = new TestPath("TestPath");

        // Act & Assert
        path.ShouldBeAssignableTo<PathBase>();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPath : PathBase
    {
        public TestPath(string name) : base(name)
        {
        }

        public override string PathValue => "/test/path/value";
        public override string Domain => "Sql";
    }
}
