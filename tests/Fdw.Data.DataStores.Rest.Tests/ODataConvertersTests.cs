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
}
