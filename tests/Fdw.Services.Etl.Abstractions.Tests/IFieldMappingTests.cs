using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// Tests for IFieldMapping interface contract.
/// </summary>
public class IFieldMappingTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void SourceFieldPropertyCanBeRead()
    {
        // Arrange
        const string expected = "SourceField";
        var mapping = new TestFieldMapping { SourceField = expected };

        // Act
        var result = mapping.SourceField;

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DestinationFieldPropertyCanBeRead()
    {
        // Arrange
        const string expected = "DestinationField";
        var mapping = new TestFieldMapping { DestinationField = expected };

        // Act
        var result = mapping.DestinationField;

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TransformExpressionPropertyCanBeRead()
    {
        // Arrange
        const string expected = "x * 2";
        var mapping = new TestFieldMapping { TransformExpression = expected };

        // Act
        var result = mapping.TransformExpression;

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TransformExpressionPropertyCanBeNull()
    {
        // Arrange
        var mapping = new TestFieldMapping { TransformExpression = null };

        // Act
        var result = mapping.TransformExpression;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultValuePropertyCanBeRead()
    {
        // Arrange
        const string expected = "DefaultValue";
        var mapping = new TestFieldMapping { DefaultValue = expected };

        // Act
        var result = mapping.DefaultValue;

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultValuePropertyCanBeNull()
    {
        // Arrange
        var mapping = new TestFieldMapping { DefaultValue = null };

        // Act
        var result = mapping.DefaultValue;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TargetTypePropertyCanBeRead()
    {
        // Arrange
        const string expected = "int";
        var mapping = new TestFieldMapping { TargetType = expected };

        // Act
        var result = mapping.TargetType;

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TargetTypePropertyCanBeNull()
    {
        // Arrange
        var mapping = new TestFieldMapping { TargetType = null };

        // Act
        var result = mapping.TargetType;

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("bool")]
    [InlineData("datetime")]
    [InlineData("guid")]
    public void TargetTypeSupportsStandardTypes(string targetType)
    {
        // Arrange
        var mapping = new TestFieldMapping { TargetType = targetType };

        // Act
        var result = mapping.TargetType;

        // Assert
        result.ShouldBe(targetType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IsRequiredPropertyCanBeRead()
    {
        // Arrange
        var mapping = new TestFieldMapping { IsRequired = true };

        // Act
        var result = mapping.IsRequired;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IsEnabledPropertyCanBeRead()
    {
        // Arrange
        var mapping = new TestFieldMapping { IsEnabled = true };

        // Act
        var result = mapping.IsEnabled;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllPropertiesCanBeSetTogether()
    {
        // Arrange
        var mapping = new TestFieldMapping
        {
            SourceField = "Source",
            DestinationField = "Destination",
            TransformExpression = "x + 1",
            DefaultValue = "0",
            TargetType = "int",
            IsRequired = true,
            IsEnabled = true
        };

        // Act & Assert
        mapping.SourceField.ShouldBe("Source");
        mapping.DestinationField.ShouldBe("Destination");
        mapping.TransformExpression.ShouldBe("x + 1");
        mapping.DefaultValue.ShouldBe("0");
        mapping.TargetType.ShouldBe("int");
        mapping.IsRequired.ShouldBeTrue();
        mapping.IsEnabled.ShouldBeTrue();
    }

    /// <summary>
    /// Test implementation of IFieldMapping.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestFieldMapping : IFieldMapping
    {
        public string SourceField { get; set; } = string.Empty;
        public string DestinationField { get; set; } = string.Empty;
        public string? TransformExpression { get; set; }
        public string? DefaultValue { get; set; }
        public string? TargetType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsEnabled { get; set; }

        public IReadOnlyList<IFieldMappingTransform> Transforms { get; set; } = [];
    }
}
