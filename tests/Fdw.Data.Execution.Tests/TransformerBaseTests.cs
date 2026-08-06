using System.Collections.Generic;
using System.Threading;
using Fdw.Data.Transformers.Abstractions;
using Fdw.Results;

namespace Fdw.Data.Execution.Tests;

/// <summary>
/// Tests for <see cref="TransformerBase{TIn,TOut}"/>.
/// </summary>
public sealed class TransformerBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsIdAndName()
    {
        // Arrange & Act
        var sut = new TestTransformer(42, "MyTransformer");

        // Assert
        sut.Id.ShouldBe(42);
        sut.Name.ShouldBe("MyTransformer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSourceAndTargetTypes()
    {
        // Arrange & Act
        var sut = new TestTransformer(1, "TypedTransformer");

        // Assert
        sut.SourceType.ShouldBe(typeof(string));
        sut.TargetType.ShouldBe(typeof(int));
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorThrowsWhenNameIsNullOrWhitespace(string? name)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new TestTransformer(1, name!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerImplementsIDataTransformerInterface()
    {
        // Arrange & Act
        var sut = new TestTransformer(1, "Test");

        // Assert
        sut.ShouldBeAssignableTo<IDataTransformer>();
        sut.ShouldBeAssignableTo<IDataTransformer<string, int>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TransformReturnsSuccessResultForValidInput()
    {
        // Arrange
        var sut = new TestTransformer(1, "Test");
        var source = new[] { "1", "2", "3" };
        var context = new TransformContext();

        // Act
        var result = sut.Transform(source, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBe([1, 2, 3]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TransformReturnsEmptyCollectionForEmptyInput()
    {
        // Arrange
        var sut = new TestTransformer(1, "Test");
        var source = Array.Empty<string>();
        var context = new TransformContext();

        // Act
        var result = sut.Transform(source, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MultipleTransformerInstancesHaveIndependentState()
    {
        // Arrange
        var transformer1 = new TestTransformer(1, "First");
        var transformer2 = new TestTransformer(2, "Second");

        // Assert
        transformer1.Id.ShouldNotBe(transformer2.Id);
        transformer1.Name.ShouldNotBe(transformer2.Name);
    }

    /// <summary>
    /// Concrete test implementation of TransformerBase.
    /// Converts strings to ints.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private sealed class TestTransformer : TransformerBase<string, int>
    {
        public TestTransformer(int id, string name) : base(id, name)
        {
        }

        public override IGenericResult<IEnumerable<int>> Transform(
            IEnumerable<string> source,
            TransformContext context,
            CancellationToken cancellationToken = default)
        {
            var results = new List<int>();
            foreach (var item in source)
            {
                if (int.TryParse(item, out var value))
                {
                    results.Add(value);
                }
            }
            return GenericResult<IEnumerable<int>>.Success(results);
        }
    }
}
