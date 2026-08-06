using Xunit;
using Shouldly;

namespace Fdw.Data.DataStores.Rest.Tests;

/// <summary>
/// Tests for <see cref="ODataConverters.BySourceType(string)"/>.
/// </summary>
public sealed class ODataConvertersTests
{
    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BySourceTypeReturnsNotFoundForNullOrWhitespace(string? sourceType)
    {
        // Act
        var converter = ODataConverters.BySourceType(sourceType!);

        // Assert
        converter.ShouldBe(ODataConverters.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeReturnsNotFoundForUnknownType()
    {
        // Act
        var converter = ODataConverters.BySourceType("SomeUnknownEdmType");

        // Assert
        converter.ShouldBe(ODataConverters.NotFound);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Boolean", typeof(bool))]
    [InlineData("Int32", typeof(int))]
    [InlineData("Double", typeof(double))]
    [InlineData("DateTimeOffset", typeof(System.DateTimeOffset))]
    public void BySourceTypeResolvesKnownEdmTypesToClrType(string sourceType, System.Type expectedClrType)
    {
        // Act
        var converter = ODataConverters.BySourceType(sourceType);

        // Assert
        converter.ShouldNotBe(ODataConverters.NotFound);
        converter.SourceType.ShouldBe(sourceType);
        converter.TargetClrType.ShouldBe(expectedClrType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeComparisonIsCaseSensitive()
    {
        // Act
        // Why: BySourceType compares with StringComparison.Ordinal — EDM type names are
        // PascalCase ("Boolean") and a lowercase variant must not accidentally match.
        var converter = ODataConverters.BySourceType("boolean");

        // Assert
        converter.ShouldBe(ODataConverters.NotFound);
    }
}
