using System;
using Fdw.Data.Transformations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// Tests for FromUnixMillisecondsFieldTransformer.
/// </summary>
public sealed class FromUnixMillisecondsFieldTransformerTests
{
    private static readonly TransformationContext EmptyContext = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_WithEpochMilliseconds_ReturnsExpectedDateTimeOffset()
    {
        // Arrange: 1700000000000 ms = 2023-11-14T22:13:20+00:00
        var transformer = new FromUnixMillisecondsFieldTransformer();
        var expected = new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero);

        // Act
        var result = await transformer.Transform(1700000000000L, EmptyContext, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var value = result.Value.ShouldBeOfType<DateTimeOffset>();
        value.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_WithStringEpochMilliseconds_ReturnsExpectedDateTimeOffset()
    {
        // Arrange: numeric string should also be convertible to long
        var transformer = new FromUnixMillisecondsFieldTransformer();
        var expected = new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero);

        // Act
        var result = await transformer.Transform("1700000000000", EmptyContext, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_WithNullInput_ReturnsFailure()
    {
        // Arrange
        var transformer = new FromUnixMillisecondsFieldTransformer();

        // Act
        var result = await transformer.Transform(null, EmptyContext, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_WithNonNumericString_ReturnsFailure()
    {
        // Arrange
        var transformer = new FromUnixMillisecondsFieldTransformer();

        // Act
        var result = await transformer.Transform("not-a-number", EmptyContext, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_WithNonConvertibleType_ReturnsFailure()
    {
        // Arrange: a boolean is not convertible to long
        var transformer = new FromUnixMillisecondsFieldTransformer();

        // Act
        var result = await transformer.Transform(true, EmptyContext, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_WithZeroEpoch_ReturnsUnixEpochStart()
    {
        // Arrange: epoch 0 ms = DateTimeOffset.UnixEpoch
        var transformer = new FromUnixMillisecondsFieldTransformer();

        // Act
        var result = await transformer.Transform(0L, EmptyContext, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_WithDoubleInput_ReturnsFailure()
    {
        // Arrange: a raw double that cannot be safely cast to long without explicit conversion
        // should fail loud rather than silently truncate precision
        var transformer = new FromUnixMillisecondsFieldTransformer();

        // Act
        var result = await transformer.Transform(1700000000.5, EmptyContext, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeOptionName_IsFromUnixMilliseconds()
    {
        var transformer = new FromUnixMillisecondsFieldTransformer();
        transformer.Name.ShouldBe("FromUnixMilliseconds");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Category_IsDateTime()
    {
        var transformer = new FromUnixMillisecondsFieldTransformer();
        transformer.Category.ShouldBe("DateTime");
    }

}
