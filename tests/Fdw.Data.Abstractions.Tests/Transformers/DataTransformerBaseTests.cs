using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Transformers;

public sealed class DataTransformerBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsIdAndName()
    {
        // Arrange & Act
        var transformer = new TestTransformer(1, "TestTransform");

        // Assert
        transformer.Id.ShouldBe(1);
        transformer.Name.ShouldBe("TestTransform");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDefaultCategory()
    {
        // Arrange & Act
        var transformer = new TestTransformer(1, "TestTransform");

        // Assert
        transformer.Category.ShouldBe("Transformer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorAcceptsCustomCategory()
    {
        // Arrange & Act
        var transformer = new TestTransformer(2, "CustomTransform", "CustomCategory");

        // Assert
        transformer.Category.ShouldBe("CustomCategory");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerNameReturnsImplementationValue()
    {
        // Arrange
        var transformer = new TestTransformer(1, "TestTransform");

        // Act
        var name = transformer.TransformerName;

        // Assert
        name.ShouldBe("Test Transformer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MultipleInstancesWithDifferentIdsAreDistinct()
    {
        // Arrange
        var transformer1 = new TestTransformer(1, "Transform1");
        var transformer2 = new TestTransformer(2, "Transform2");

        // Act & Assert
        transformer1.Id.ShouldNotBe(transformer2.Id);
        transformer1.Name.ShouldNotBe(transformer2.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var transformer = new TestTransformer(1, "TestTransform");

        // Act & Assert
        transformer.ShouldBeAssignableTo<DataTransformerBase>();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestTransformer : DataTransformerBase
    {
        public TestTransformer(int id, string name, string? category = "Transformer")
            : base(id, name, category)
        {
        }

        public override string TransformerName => "Test Transformer";
    }
}
