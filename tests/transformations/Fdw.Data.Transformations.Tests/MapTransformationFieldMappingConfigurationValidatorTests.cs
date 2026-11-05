using Fdw.Data.Transformations.Configuration;
using Shouldly;
using Xunit;

namespace Fdw.Data.Transformations.Tests;

/// <summary>
/// Whether a field mapping runs is the caller's choice (FDW-3823): a mapping that leaves it out is refused, and either stated value is kept.
/// </summary>
public sealed class MapTransformationFieldMappingConfigurationValidatorTests
{
    private static MapTransformationFieldMappingConfiguration Mapping(bool? isEnabled)
        => new() { Name = "m", SourceField = "a", DestinationField = "b", Ordinal = 0, IsEnabled = isEnabled };

    [Fact]
    [Trait("Priority", "P0")]
    public void AMappingThatLeavesOutIsEnabledIsRefused()
    {
        var result = new MapTransformationFieldMappingConfigurationValidator().Validate(Mapping(null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(MapTransformationFieldMappingConfiguration.IsEnabled));
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData(true)]
    [InlineData(false)]
    public void AStatedIsEnabledIsAccepted(bool isEnabled)
        => new MapTransformationFieldMappingConfigurationValidator().Validate(Mapping(isEnabled)).IsValid.ShouldBeTrue();
}
