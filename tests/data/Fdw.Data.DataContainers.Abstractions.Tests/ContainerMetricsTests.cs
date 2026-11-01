using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class ContainerMetricsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        const long recordCount = 1000;
        const long dataSize = 50000;
        var lastModified = DateTimeOffset.UtcNow;
        var additionalMetrics = new Dictionary<string, object>
        {
            ["CompressionRatio"] = 0.75,
            ["IndexCount"] = 3
        };

        // Act
        var metrics = new ContainerMetrics(recordCount, dataSize, lastModified, additionalMetrics);

        // Assert
        metrics.EstimatedRecordCount.ShouldBe(recordCount);
        metrics.EstimatedDataSize.ShouldBe(dataSize);
        metrics.LastModified.ShouldBe(lastModified);
        metrics.AdditionalMetrics.ShouldNotBeNull();
        metrics.AdditionalMetrics.Count.ShouldBe(2);
        metrics.AdditionalMetrics["CompressionRatio"].ShouldBe(0.75);
        metrics.AdditionalMetrics["IndexCount"].ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullLastModifiedSetsNullValue()
    {
        // Arrange & Act
        var metrics = new ContainerMetrics(100, 5000, null);

        // Assert
        metrics.LastModified.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullAdditionalMetricsCreatesEmptyDictionary()
    {
        // Arrange & Act
        var metrics = new ContainerMetrics(100, 5000, null, null);

        // Assert
        metrics.AdditionalMetrics.ShouldNotBeNull();
        metrics.AdditionalMetrics.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AdditionalMetricsUsesOrdinalComparer()
    {
        // Arrange
        var additionalMetrics = new Dictionary<string, object>
        {
            ["Key"] = "value"
        };

        // Act
        var metrics = new ContainerMetrics(0, 0, null, additionalMetrics);

        // Assert
        metrics.AdditionalMetrics.ContainsKey("Key").ShouldBeTrue();
        metrics.AdditionalMetrics.ContainsKey("key").ShouldBeFalse();
        metrics.AdditionalMetrics.ContainsKey("KEY").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithZeroValuesWorksCorrectly()
    {
        // Arrange & Act
        var metrics = new ContainerMetrics(0, 0);

        // Assert
        metrics.EstimatedRecordCount.ShouldBe(0);
        metrics.EstimatedDataSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNegativeValuesAllowsNegativeOne()
    {
        // Arrange & Act
        var metrics = new ContainerMetrics(-1, -1);

        // Assert
        metrics.EstimatedRecordCount.ShouldBe(-1);
        metrics.EstimatedDataSize.ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AdditionalMetricsIsIndependentCopy()
    {
        // Arrange
        var original = new Dictionary<string, object>
        {
            ["Original"] = "value"
        };
        var metrics = new ContainerMetrics(100, 5000, null, original);

        // Act
        original["Modified"] = "newValue";

        // Assert
        metrics.AdditionalMetrics.ContainsKey("Original").ShouldBeTrue();
        metrics.AdditionalMetrics.ContainsKey("Modified").ShouldBeFalse();
    }
}
