using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class ContainerMetadataTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var name = "TestContainer";
        long sizeBytes = 1024;
        var createdDate = DateTime.UtcNow.AddDays(-7);
        var modifiedDate = DateTime.UtcNow;
        var additionalMetadata = new Dictionary<string, object>
        {
            ["Version"] = "1.0",
            ["Format"] = "CSV"
        };

        // Act
        var metadata = new ContainerMetadata(name, sizeBytes, createdDate, modifiedDate, additionalMetadata);

        // Assert
        metadata.Name.ShouldBe(name);
        metadata.SizeBytes.ShouldBe(sizeBytes);
        metadata.CreatedDate.ShouldBe(createdDate);
        metadata.ModifiedDate.ShouldBe(modifiedDate);
        metadata.AdditionalMetadata.ShouldNotBeNull();
        metadata.AdditionalMetadata.Count.ShouldBe(2);
        metadata.AdditionalMetadata["Version"].ShouldBe("1.0");
        metadata.AdditionalMetadata["Format"].ShouldBe("CSV");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullSizeBytesInitializesCorrectly()
    {
        // Arrange & Act
        var metadata = new ContainerMetadata("Test");

        // Assert
        metadata.Name.ShouldBe("Test");
        metadata.SizeBytes.ShouldBeNull();
        metadata.CreatedDate.ShouldBeNull();
        metadata.ModifiedDate.ShouldBeNull();
        metadata.AdditionalMetadata.ShouldNotBeNull();
        metadata.AdditionalMetadata.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullCreatedDateInitializesCorrectly()
    {
        // Arrange & Act
        var metadata = new ContainerMetadata("Test", 1000, null);

        // Assert
        metadata.Name.ShouldBe("Test");
        metadata.SizeBytes.ShouldBe(1000);
        metadata.CreatedDate.ShouldBeNull();
        metadata.ModifiedDate.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullModifiedDateInitializesCorrectly()
    {
        // Arrange
        var createdDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var metadata = new ContainerMetadata("Test", 1000, createdDate, null);

        // Assert
        metadata.Name.ShouldBe("Test");
        metadata.SizeBytes.ShouldBe(1000);
        metadata.CreatedDate.ShouldBe(createdDate);
        metadata.ModifiedDate.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullAdditionalMetadataCreatesEmptyDictionary()
    {
        // Arrange & Act
        var metadata = new ContainerMetadata("Test", 1000, null, null, null);

        // Assert
        metadata.AdditionalMetadata.ShouldNotBeNull();
        metadata.AdditionalMetadata.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AdditionalMetadataUsesOrdinalComparer()
    {
        // Arrange
        var additionalMetadata = new Dictionary<string, object>
        {
            ["Key"] = "value"
        };

        // Act
        var metadata = new ContainerMetadata("Test", null, null, null, additionalMetadata);

        // Assert
        metadata.AdditionalMetadata.ContainsKey("Key").ShouldBeTrue();
        metadata.AdditionalMetadata.ContainsKey("key").ShouldBeFalse();
        metadata.AdditionalMetadata.ContainsKey("KEY").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AdditionalMetadataIsReadOnlyReference()
    {
        // Arrange
        var original = new Dictionary<string, object>
        {
            ["Original"] = "value"
        };
        var metadata = new ContainerMetadata("Test", null, null, null, original);

        // Act
        original["Modified"] = "newValue";

        // Assert - Since it's just a cast to IReadOnlyDictionary, mutations to original ARE visible
        metadata.AdditionalMetadata.ContainsKey("Original").ShouldBeTrue();
        metadata.AdditionalMetadata.ContainsKey("Modified").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithZeroSizeBytesWorksCorrectly()
    {
        // Arrange & Act
        var metadata = new ContainerMetadata("Test", 0);

        // Assert
        metadata.SizeBytes.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithLargeSizeBytesWorksCorrectly()
    {
        // Arrange
        long largeSize = long.MaxValue;

        // Act
        var metadata = new ContainerMetadata("Test", largeSize);

        // Assert
        metadata.SizeBytes.ShouldBe(largeSize);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithEmptyAdditionalMetadataCreatesEmptyDictionary()
    {
        // Arrange
        var emptyMetadata = new Dictionary<string, object>();

        // Act
        var metadata = new ContainerMetadata("Test", null, null, null, emptyMetadata);

        // Assert
        metadata.AdditionalMetadata.ShouldNotBeNull();
        metadata.AdditionalMetadata.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorPreservesComplexObjectsInAdditionalMetadata()
    {
        // Arrange
        var complexObject = new { Prop1 = "value1", Prop2 = 42 };
        var additionalMetadata = new Dictionary<string, object>
        {
            ["Complex"] = complexObject
        };

        // Act
        var metadata = new ContainerMetadata("Test", null, null, null, additionalMetadata);

        // Assert
        metadata.AdditionalMetadata["Complex"].ShouldBe(complexObject);
    }
}
