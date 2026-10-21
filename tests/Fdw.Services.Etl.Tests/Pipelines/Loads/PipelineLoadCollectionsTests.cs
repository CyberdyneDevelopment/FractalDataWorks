using Fdw.Commands.Data.SoftDeletes;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for the vocabularies a pipeline's load configuration is validated against (FDW-1108). Each option states
/// the rules it carries as flags, so the validator never branches on an option's name.
/// </summary>
public sealed class PipelineLoadCollectionsTests
{
    [Theory]
    [InlineData("BulkInsert", false, false, true, false, false, false, false, true)]
    [InlineData("BulkUpsert", true, true, true, true, true, false, false, true)]
    [InlineData("Update", true, false, true, false, true, false, false, true)]
    [InlineData("Delete", true, false, false, false, true, false, false, true)]
    [InlineData("Truncate", false, false, false, false, false, false, false, false)]
    [InlineData("InsertFromQuery", false, false, true, false, false, true, false, false)]
    [InlineData("Execute", false, false, false, false, false, false, true, false)]
    public void LoadStepKindStatesWhichOptionsItTakes(
        string name, bool keys, bool compares, bool writes, bool versioning, bool softDelete, bool sourceDataSet, bool container, bool rows)
    {
        // Act
        var kind = LoadStepKinds.ByName(name);

        // Assert
        ReferenceEquals(kind, LoadStepKinds.NotFound).ShouldBeFalse();
        kind.UsesKeyColumns.ShouldBe(keys);
        kind.UsesCompareColumns.ShouldBe(compares);
        kind.UsesWriteColumns.ShouldBe(writes);
        kind.AllowsVersioning.ShouldBe(versioning);
        kind.AllowsSoftDelete.ShouldBe(softDelete);
        kind.UsesSourceDataSet.ShouldBe(sourceDataSet);
        kind.UsesDestinationContainer.ShouldBe(container);
        kind.UsesRows.ShouldBe(rows);
    }

    [Theory]
    [InlineData("Key", false, true, false, false)]
    [InlineData("Compare", false, false, true, false)]
    [InlineData("Write", false, false, false, true)]
    [InlineData("SequenceOrder", true, false, false, false)]
    public void ColumnRoleStatesWhereItBelongsAndWhatItIs(string name, bool belongsToValueSource, bool isMatchKey, bool isComparison, bool isWritten)
    {
        // Act
        var role = PipelineLoadColumnRoles.ByName(name);

        // Assert
        ReferenceEquals(role, PipelineLoadColumnRoles.NotFound).ShouldBeFalse();
        role.BelongsToValueSource.ShouldBe(belongsToValueSource);
        role.IsMatchKey.ShouldBe(isMatchKey);
        role.IsComparison.ShouldBe(isComparison);
        role.IsWritten.ShouldBe(isWritten);
    }

    [Theory]
    [InlineData("Column", true, false, true, false, false, false)]
    [InlineData("NextSequenceValue", false, false, false, true, true, false)]
    [InlineData("CurrentTimestamp", false, true, false, false, false, false)]
    [InlineData("Constant", false, true, false, false, false, true)]
    public void ValueSourceKindStatesWhatItNeeds(
        string name, bool mapsSourceColumn, bool allowedAsContainerParameter, bool sourceField, bool sequenceName, bool sequenceOrder, bool constant)
    {
        // Act
        var kind = ValueSourceKinds.ByName(name);

        // Assert
        ReferenceEquals(kind, ValueSourceKinds.NotFound).ShouldBeFalse();
        kind.MapsSourceColumn.ShouldBe(mapsSourceColumn);
        kind.AllowedAsContainerParameter.ShouldBe(allowedAsContainerParameter);
        kind.RequiresSourceField.ShouldBe(sourceField);
        kind.RequiresSequenceName.ShouldBe(sequenceName);
        kind.RequiresSequenceOrder.ShouldBe(sequenceOrder);
        kind.RequiresConstant.ShouldBe(constant);
    }

    [Theory]
    [InlineData("MarkerMatches", true)]
    [InlineData("AbsentFromSource", false)]
    public void SoftDeleteModeStatesWhetherItNeedsAMarker(string name, bool requiresMarker)
    {
        // Act
        var mode = SoftDeleteModes.ByName(name);

        // Assert
        ReferenceEquals(mode, SoftDeleteModes.NotFound).ShouldBeFalse();
        mode.RequiresMarker.ShouldBe(requiresMarker);
    }

    [Theory]
    [InlineData("String")]
    [InlineData("Int32")]
    [InlineData("Int64")]
    [InlineData("Decimal")]
    [InlineData("Double")]
    [InlineData("DateTimeOffset")]
    [InlineData("Bool")]
    [InlineData("Guid")]
    public void EveryDataTypeARunParameterCanHaveExistsAndFitsAnExpression(string name)
    {
        var type = Fdw.Data.Abstractions.DataTypes.ByName(name);

        ReferenceEquals(type, Fdw.Data.Abstractions.DataTypes.NotFound).ShouldBeFalse();
        type.RendersInExpressions.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Load", false)]
    [InlineData("Reject", true)]
    public void LoadPhaseStatesWhetherItWritesRejectedRecords(string name, bool readsRejected)
    {
        // Act
        var phase = LoadPhases.ByName(name);

        // Assert
        ReferenceEquals(phase, LoadPhases.NotFound).ShouldBeFalse();
        phase.ReadsRejectedRecords.ShouldBe(readsRejected);
    }

    [Theory]
    [InlineData("BulkInsert", true)]
    [InlineData("BulkUpsert", false)]
    [InlineData("Update", false)]
    [InlineData("Delete", false)]
    [InlineData("Truncate", false)]
    [InlineData("InsertFromQuery", false)]
    [InlineData("Execute", false)]
    public void LoadStepKindStatesWhetherItCanWriteRejectedRecords(string name, bool accepts)
    {
        // Act
        var kind = LoadStepKinds.ByName(name);

        // Assert
        kind.AcceptsRejectedRecords.ShouldBe(accepts);
    }
}
