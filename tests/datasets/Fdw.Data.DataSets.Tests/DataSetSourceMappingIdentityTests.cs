using System;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// The one rule every path that replaces a source's field mappings uses (FDW-877): a mapping keeps the
/// Id of the current mapping for the same field, and a replacement set names each field once.
/// </summary>
public class DataSetSourceMappingIdentityTests
{
    private static readonly Guid CurrentId = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000001");
    private static readonly Guid RetiredId = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000002");

    private static DataSetSourceConfiguration SourceWithMappings() => new()
    {
        SourceName = "Teams",
        Mappings =
        [
            new DataSetFieldMappingConfiguration { Id = CurrentId, LogicalFieldName = "TeamName", PhysicalFieldName = "team_name" },
            new DataSetFieldMappingConfiguration { Id = RetiredId, LogicalFieldName = "City", IsCurrent = false },
        ],
    };

    [Fact]
    [Trait("Category", "DataSets.FDW877")]
    public void MappingIdFor_AFieldTheSourceMaps_KeepsTheCurrentMappingsId()
        => SourceWithMappings().MappingIdFor("TeamName").ShouldBe(CurrentId);

    [Fact]
    [Trait("Category", "DataSets.FDW877")]
    public void MappingIdFor_ANewField_IsUnsetSoTheCascadeMintsOne()
        => SourceWithMappings().MappingIdFor("Conference").ShouldBe(Guid.Empty);

    [Fact]
    [Trait("Category", "DataSets.FDW877")]
    public void MappingIdFor_AFieldOnlyARetiredMappingFilled_IsUnset()
        => SourceWithMappings().MappingIdFor("City").ShouldBe(Guid.Empty);

    [Fact]
    [Trait("Category", "DataSets.FDW877")]
    public void MappingIdFor_ComparesTheFieldNameExactly()
        => SourceWithMappings().MappingIdFor("teamname").ShouldBe(Guid.Empty);

    [Fact]
    [Trait("Category", "DataSets.FDW877")]
    public void ValidateMappingFields_DistinctNamedFields_Succeeds()
        => DataSetSourceConfiguration.ValidateMappingFields(
                ["TeamName", "City"], "NflTeams", "Teams", NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();

    [Fact]
    [Trait("Category", "DataSets.FDW877")]
    public void ValidateMappingFields_AMappingWithNoField_Fails()
        => DataSetSourceConfiguration.ValidateMappingFields(
                ["TeamName", " "], "NflTeams", "Teams", NullLogger.Instance)
            .IsFailure.ShouldBeTrue();

    [Fact]
    [Trait("Category", "DataSets.FDW877")]
    public void ValidateMappingFields_AFieldMappedTwice_Fails()
        => DataSetSourceConfiguration.ValidateMappingFields(
                ["TeamName", "City", "TeamName"], "NflTeams", "Teams", NullLogger.Instance)
            .IsFailure.ShouldBeTrue();
}
