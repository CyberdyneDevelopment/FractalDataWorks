using System.Collections.Generic;
using System.Threading;
using Fdw.Data.Transformers.Abstractions;
using Fdw.Results;

namespace Fdw.Data.DataStores.FileSystem.Tests;

/// <summary>
/// Tests for <see cref="TransformerBase{TIn,TOut}"/> and <see cref="TransformContext"/>.
/// Covers the TransformerBase contract and TransformContext behavior.
/// </summary>
public sealed class DataTransformerTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerBaseConstructorSetsIdCorrectly()
    {
        // Arrange & Act
        var sut = new StringToUpperTransformer(7, "ToUpper");

        // Assert
        sut.Id.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerBaseConstructorSetsNameCorrectly()
    {
        // Arrange & Act
        var sut = new StringToUpperTransformer(1, "ToUpperCase");

        // Assert
        sut.Name.ShouldBe("ToUpperCase");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerBaseConstructorSetsSourceTypeToTIn()
    {
        // Arrange & Act
        var sut = new StringToUpperTransformer(1, "Test");

        // Assert
        sut.SourceType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerBaseConstructorSetsTargetTypeToTOut()
    {
        // Arrange & Act
        var sut = new StringToUpperTransformer(1, "Test");

        // Assert
        sut.TargetType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerBaseThrowsArgumentExceptionForNullName()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new StringToUpperTransformer(1, null!));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerBaseThrowsArgumentExceptionForEmptyName()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new StringToUpperTransformer(1, string.Empty));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerBaseThrowsArgumentExceptionForWhitespaceName()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new StringToUpperTransformer(1, "   "));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerImplementsNonGenericInterface()
    {
        // Arrange
        var sut = new StringToUpperTransformer(1, "Test");

        // Assert
        sut.ShouldBeAssignableTo<IDataTransformer>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformerImplementsGenericInterface()
    {
        // Arrange
        var sut = new StringToUpperTransformer(1, "Test");

        // Assert
        sut.ShouldBeAssignableTo<IDataTransformer<string, string>>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformConvertsInputRecords()
    {
        // Arrange
        var sut = new StringToUpperTransformer(1, "ToUpper");
        var input = new[] { "hello", "world" };
        var context = new TransformContext { SourceName = "Test" };

        // Act
        var result = sut.Transform(input, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBe(["HELLO", "WORLD"]);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformHandlesEmptyInputCollection()
    {
        // Arrange
        var sut = new StringToUpperTransformer(1, "ToUpper");
        var input = Array.Empty<string>();
        var context = new TransformContext();

        // Act
        var result = sut.Transform(input, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformHandlesSingleElementCollection()
    {
        // Arrange
        var sut = new StringToUpperTransformer(1, "ToUpper");
        var input = new[] { "single" };
        var context = new TransformContext();

        // Act
        var result = sut.Transform(input, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count().ShouldBe(1);
        result.Value.First().ShouldBe("SINGLE");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformContextDefaultsThrowOnErrorToTrue()
    {
        // Arrange
        var context = new TransformContext();

        // Assert
        context.ThrowOnError.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformContextAllowsSettingThrowOnErrorToFalse()
    {
        // Arrange
        var context = new TransformContext { ThrowOnError = false };

        // Assert
        context.ThrowOnError.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformContextSourceNameCanBeAssigned()
    {
        // Arrange
        var context = new TransformContext { SourceName = "MySource" };

        // Assert
        context.SourceName.ShouldBe("MySource");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformContextConnectionTypeCanBeAssigned()
    {
        // Arrange
        var context = new TransformContext { ConnectionType = "REST" };

        // Assert
        context.ConnectionType.ShouldBe("REST");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformContextMetadataIsInitializedEmpty()
    {
        // Arrange & Act
        var context = new TransformContext();

        // Assert
        context.Metadata.ShouldNotBeNull();
        context.Metadata.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformContextMetadataCanBePopulatedFromDictionary()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "BatchId", Guid.NewGuid() },
            { "RecordCount", 500 }
        };

        // Act
        var context = new TransformContext(metadata);

        // Assert
        context.Metadata.Count.ShouldBe(2);
        context.Metadata.ContainsKey("BatchId").ShouldBeTrue();
        context.Metadata.ContainsKey("RecordCount").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TransformContextMetadataSupportsRuntimeAddition()
    {
        // Arrange
        var context = new TransformContext();

        // Act
        context.Metadata["RunId"] = "run-001";
        context.Metadata["Status"] = "Running";

        // Assert
        context.Metadata.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void DifferentTransformerInstancesAreIndependent()
    {
        // Arrange
        var transformer1 = new StringToUpperTransformer(1, "First");
        var transformer2 = new StringToUpperTransformer(2, "Second");

        // Assert
        transformer1.ShouldNotBeSameAs(transformer2);
        transformer1.Id.ShouldNotBe(transformer2.Id);
    }

    /// <summary>
    /// Concrete implementation of TransformerBase for testing purposes.
    /// Converts strings to uppercase.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private sealed class StringToUpperTransformer : TransformerBase<string, string>
    {
        public StringToUpperTransformer(int id, string name) : base(id, name)
        {
        }

        public override IGenericResult<IEnumerable<string>> Transform(
            IEnumerable<string> source,
            TransformContext context,
            CancellationToken cancellationToken = default)
        {
            var results = source.Select(s => s.ToUpperInvariant()).ToList();
            return GenericResult<IEnumerable<string>>.Success(results);
        }
    }
}
