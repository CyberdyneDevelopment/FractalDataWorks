using Fdw.Data.Transformers.Abstractions;

namespace Fdw.Data.Execution.Tests;

/// <summary>
/// Tests for <see cref="TransformContext"/>.
/// </summary>
public sealed class TransformContextTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultConstructorInitializesEmptyMetadata()
    {
        // Arrange & Act
        var sut = new TransformContext();

        // Assert
        sut.Metadata.ShouldNotBeNull();
        sut.Metadata.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultConstructorInitializesDefaultPropertyValues()
    {
        // Arrange & Act
        var sut = new TransformContext();

        // Assert
        sut.SourceName.ShouldBe(string.Empty);
        sut.ConnectionType.ShouldBe(string.Empty);
        sut.ThrowOnError.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataConstructorCopiesProvidedMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "Key1", "Value1" },
            { "Key2", 42 }
        };

        // Act
        var sut = new TransformContext(metadata);

        // Assert
        sut.Metadata.ShouldNotBeNull();
        sut.Metadata.Count.ShouldBe(2);
        sut.Metadata["Key1"].ShouldBe("Value1");
        sut.Metadata["Key2"].ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataIsCaseInsensitive()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "MyKey", "Value" }
        };

        // Act
        var sut = new TransformContext(metadata);

        // Assert
        sut.Metadata.ContainsKey("mykey").ShouldBeTrue();
        sut.Metadata.ContainsKey("MYKEY").ShouldBeTrue();
        sut.Metadata.ContainsKey("MyKey").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataConstructorDoesNotShareReferenceWithInput()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "Key1", "Value1" }
        };
        var sut = new TransformContext(metadata);

        // Act - modify original
        metadata["Key2"] = "Value2";

        // Assert - context should not reflect the change
        sut.Metadata.ShouldNotContainKey("Key2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SourceNameCanBeSet()
    {
        // Arrange
        var sut = new TransformContext();

        // Act
        sut.SourceName = "TestSource";

        // Assert
        sut.SourceName.ShouldBe("TestSource");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConnectionTypeCanBeSet()
    {
        // Arrange
        var sut = new TransformContext();

        // Act
        sut.ConnectionType = "SQL";

        // Assert
        sut.ConnectionType.ShouldBe("SQL");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ThrowOnErrorCanBeSetToFalse()
    {
        // Arrange
        var sut = new TransformContext();

        // Act
        sut.ThrowOnError = false;

        // Assert
        sut.ThrowOnError.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataAcceptsAddedEntries()
    {
        // Arrange
        var sut = new TransformContext();

        // Act
        sut.Metadata["ProcessingDate"] = DateTimeOffset.UtcNow;
        sut.Metadata["BatchSize"] = 1000;

        // Assert
        sut.Metadata.Count.ShouldBe(2);
        sut.Metadata.ContainsKey("ProcessingDate").ShouldBeTrue();
        sut.Metadata.ContainsKey("BatchSize").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataConstructorWithEmptyDictionaryInitializesEmptyMetadata()
    {
        // Arrange
        var emptyMetadata = new Dictionary<string, object>();

        // Act
        var sut = new TransformContext(emptyMetadata);

        // Assert
        sut.Metadata.ShouldBeEmpty();
    }
}
