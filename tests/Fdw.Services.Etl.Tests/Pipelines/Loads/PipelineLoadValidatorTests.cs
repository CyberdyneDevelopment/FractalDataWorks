using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="PipelineLoadValidator"/> (FDW-1108): a load row that is incomplete or contradicts itself is
/// refused with a reason, and nothing is filled in on the caller's behalf. The tests run the checks a graph's
/// destination component runs on its row: column lists, versioning, soft delete and value sources.
/// </summary>
public sealed class PipelineLoadValidatorTests
{
    private const string Owner = "load step 'step'";

    // The checks a graph's destination component runs on its row (DestinationOptions), in the same order, for the kind the row states.
    private static IGenericResult Check(PipelineLoadStepConfiguration step)
    {
        var kind = LoadStepKinds.ByName(step.Kind);
        var columns = PipelineLoadValidator.ValidateColumnLists(Owner, step, kind, kind.Name, NullLogger.Instance);
        if (columns.IsFailure)
            return columns;
        var versioning = PipelineLoadValidator.ValidateVersioning(Owner, step, kind, kind.Name, NullLogger.Instance);
        if (versioning.IsFailure)
            return versioning;
        var softDelete = PipelineLoadValidator.ValidateSoftDelete(Owner, step, kind, kind.Name, NullLogger.Instance);
        return softDelete.IsFailure
            ? softDelete
            : PipelineLoadValidator.ValidateValueSources(Owner, step, kind.UsesSourceDataSet, kind.UsesDestinationContainer, NullLogger.Instance);
    }

    private static PipelineLoadColumnConfiguration Column(string role, string column, int ordinal = 0) =>
        new() { Name = column, Role = role, ColumnName = column, Ordinal = ordinal };

    private static PipelineLoadStepConfiguration Step(string kind, int order = 1, string name = "step")
    {
        var step = new PipelineLoadStepConfiguration { Phase = "Load", Name = name, StepOrder = order, Kind = kind, DestinationDataSet = "Orders", IsEnabled = true };
        switch (kind)
        {
            case "BulkInsert":
                step.Columns = [Column("Write", "Name")];
                break;
            case "BulkUpsert":
            case "Update":
                step.Columns = [Column("Key", "Id"), Column("Write", "Name")];
                break;
            case "Delete":
                step.Columns = [Column("Key", "Id")];
                break;
            case "InsertFromQuery":
                step.SourceDataSet = "Staging";
                step.Columns = [Column("Write", "Name")];
                break;
            case "Execute":
                step.DestinationDataStoreName = "Main";
                step.DestinationPathName = "dbo";
                step.DestinationContainerName = "LoadSummary";
                break;
        }

        return step;
    }

    private static PipelineLoadValueSourceConfiguration Source(string kind, string column = "Target") =>
        new() { Name = column, ColumnName = column, SourceKind = kind };

    private static void Fails(IGenericResult result, string reason)
    {
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain(reason);
    }

    // ---- a load row of each kind

    [Theory]
    [InlineData("BulkInsert")]
    [InlineData("BulkUpsert")]
    [InlineData("InsertFromQuery")]
    [InlineData("Update")]
    [InlineData("Delete")]
    [InlineData("Truncate")]
    [InlineData("Execute")]
    public void EveryKindIsValidWithItsOwnOptions(string kind) => Check(Step(kind)).IsSuccess.ShouldBeTrue();

    // ---- what each kind takes

    [Fact]
    public void ATruncateRefusesWriteColumns()
    {
        var step = Step("Truncate");
        step.Columns = [Column("Write", "Name")];
        Fails(Check(step), "which Truncate does not use");
    }

    [Fact]
    public void ADeleteRefusesWriteColumns()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", "Id"), Column("Write", "Name")];
        Fails(Check(step), "which Delete does not use");
    }

    [Fact]
    public void ABulkInsertRefusesKeyColumns()
    {
        var step = Step("BulkInsert");
        step.Columns = [Column("Key", "Id"), Column("Write", "Name")];
        Fails(Check(step), "which BulkInsert does not use");
    }

    [Fact]
    public void AnUpdateNeedsKeyColumns()
    {
        var step = Step("Update");
        step.Columns = [Column("Write", "Name")];
        Fails(Check(step), "key columns is needed");
    }

    [Fact]
    public void AnUpdateRefusesVersioning()
    {
        var step = Step("Update");
        step.EffectiveFromColumn = "ValidFrom";
        step.EffectiveToColumn = "ValidTo";
        Fails(Check(step), "versioning is stated, which Update does not use");
    }

    [Fact]
    public void ADeleteMayBeASoftDeleteByMarker()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "MarkerMatches";
        step.DeletedFlagColumn = "IsDeleted";
        step.SoftDeleteMarkerColumn = "Status";
        step.SoftDeleteMarkerValue = "Closed";
        Check(step).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ABulkInsertRefusesASoftDelete()
    {
        var step = Step("BulkInsert");
        step.SoftDeleteMode = "AbsentFromSource";
        step.DeletedFlagColumn = "IsDeleted";
        Fails(Check(step), "which BulkInsert does not use");
    }

    // ---- column lists

    [Fact]
    public void AColumnWithAnUnknownRoleIsRefused()
    {
        var step = Step("Delete");
        step.Columns = [Column("Index", "Id")];
        Fails(Check(step), "not a load column role");
    }

    [Fact]
    public void ASequenceOrderColumnDoesNotBelongOnAStep()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", "Id"), Column("SequenceOrder", "CreatedAt")];
        Fails(Check(step), "is not valid there");
    }

    [Fact]
    public void AColumnNeedsAName()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", " ")];
        Fails(Check(step), "has no column name");
    }

    [Fact]
    public void TwoColumnsOfOneRoleCannotShareAnOrdinal()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", "Id", 0), Column("Key", "Region", 0)];
        Fails(Check(step), "at ordinal 0");
    }

    [Fact]
    public void ACompositeKeyWithDistinctOrdinalsIsValid()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", "Id", 0), Column("Key", "Region", 1)];
        Check(step).IsSuccess.ShouldBeTrue();
    }

    // ---- versioning, insert-only rule, soft delete

    [Fact]
    public void VersioningNeedsBothColumns()
    {
        var load = Step("BulkUpsert");
        load.EffectiveFromColumn = "ValidFrom";
        Fails(Check(load), "EffectiveToColumn");
    }

    [Fact]
    public void ACurrentFlagColumnNeedsVersioning()
    {
        var load = Step("BulkUpsert");
        load.CurrentFlagColumn = "IsCurrent";
        Fails(Check(load), "EffectiveFromColumn");
    }

    [Fact]
    public void AnInsertOnlyRuleNeedsVersioning()
    {
        var load = Step("BulkUpsert");
        load.InsertOnlyRuleColumn = "Source";
        load.InsertOnlyRuleValue = "Manual";
        Fails(Check(load), "EffectiveFromColumn");
    }

    [Fact]
    public void AnInsertOnlyRuleNeedsItsValue()
    {
        var load = Step("BulkUpsert");
        load.EffectiveFromColumn = "ValidFrom";
        load.EffectiveToColumn = "ValidTo";
        load.InsertOnlyRuleColumn = "Source";
        Fails(Check(load), "InsertOnlyRuleValue");
    }

    [Fact]
    public void ASoftDeleteFlagWithoutAModeIsRefused()
    {
        var step = Step("Delete");
        step.DeletedFlagColumn = "IsDeleted";
        Fails(Check(step), "names its mode");
    }

    [Fact]
    public void ASoftDeleteModeThatDoesNotExistIsRefused()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "Never";
        step.DeletedFlagColumn = "IsDeleted";
        Fails(Check(step), "not a soft delete mode");
    }

    [Fact]
    public void ASoftDeleteNeedsItsFlagColumn()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "AbsentFromSource";
        Fails(Check(step), "names its flag column");
    }

    [Fact]
    public void AMarkerMatchesSoftDeleteNeedsItsMarker()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "MarkerMatches";
        step.DeletedFlagColumn = "IsDeleted";
        Fails(Check(step), "needs a marker column and a marker value");
    }

    [Fact]
    public void AnAbsentFromSourceSoftDeleteRefusesAMarker()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "AbsentFromSource";
        step.DeletedFlagColumn = "IsDeleted";
        step.SoftDeleteMarkerColumn = "Status";
        step.SoftDeleteMarkerValue = "Closed";
        Fails(Check(step), "does not use a marker");
    }

    [Fact]
    public void ASoftDeleteMarkerNeedsBothParts()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "MarkerMatches";
        step.DeletedFlagColumn = "IsDeleted";
        step.SoftDeleteMarkerColumn = "Status";
        Fails(Check(step), "SoftDeleteMarkerValue");
    }

    // ---- value sources

    [Fact]
    public void AStepMaySupplyEveryKindOfValueSource()
    {
        var step = Step("InsertFromQuery");
        var sequence = Source("NextSequenceValue", "Number");
        sequence.SequenceName = "dbo.OrderNumbers";
        sequence.SequenceOrderColumns = [Column("SequenceOrder", "CreatedAt")];
        var constant = Source("Constant", "Origin");
        constant.ConstantValue = "import";
        constant.ConstantDataType = "String";
        var column = Source("Column", "Label");
        column.SourceField = "Name";
        step.ValueSources = [sequence, Source("CurrentTimestamp", "LoadedAt"), constant, column];
        Check(step).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ASequenceMayBeQualifiedWithASchemaAndADatabase()
    {
        var step = Step("InsertFromQuery");
        var sequence = Source("NextSequenceValue", "Number");
        sequence.SequenceName = "OrderNumbers";
        sequence.SequenceSchema = "dbo";
        sequence.SequenceDatabase = "Sales";
        sequence.SequenceOrderColumns = [Column("SequenceOrder", "CreatedAt")];
        step.ValueSources = [sequence];
        Check(step).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ASequenceSchemaOnAConstantIsRefused()
    {
        var step = Step("BulkInsert");
        var source = Source("Constant");
        source.ConstantValue = "1";
        source.ConstantDataType = "Int32";
        source.SequenceSchema = "dbo";
        step.ValueSources = [source];
        Fails(Check(step), "a sequence schema");
    }

    [Fact]
    public void ASequenceDatabaseOnATimestampIsRefused()
    {
        var step = Step("BulkInsert");
        var source = Source("CurrentTimestamp");
        source.SequenceDatabase = "Sales";
        step.ValueSources = [source];
        Fails(Check(step), "a sequence database");
    }

    [Theory]
    [InlineData("BulkInsert")]
    [InlineData("BulkUpsert")]
    public void AColumnValueSourceIsRefusedWhereTheStepTakesRows(string kind)
    {
        var step = Step(kind);
        var source = Source("Column");
        source.SourceField = "Name";
        step.ValueSources = [source];
        Fails(Check(step), "takes a source data set");
    }

    [Fact]
    public void AColumnValueSourceNeedsItsSourceField()
    {
        var step = Step("InsertFromQuery");
        step.ValueSources = [Source("Column")];
        Fails(Check(step), "a source field");
    }

    [Fact]
    public void ASourceFieldOnAConstantIsRefused()
    {
        var step = Step("BulkInsert");
        var source = Source("Constant");
        source.ConstantValue = "1";
        source.ConstantDataType = "Int32";
        source.SourceField = "Name";
        step.ValueSources = [source];
        Fails(Check(step), "a source field");
    }

    [Fact]
    public void ANextSequenceValueNeedsASequenceName()
    {
        var step = Step("InsertFromQuery");
        var source = Source("NextSequenceValue");
        source.SequenceOrderColumns = [Column("SequenceOrder", "CreatedAt")];
        step.ValueSources = [source];
        Fails(Check(step), "a sequence name");
    }

    [Fact]
    public void ANextSequenceValueNeedsOrderingColumns()
    {
        var step = Step("InsertFromQuery");
        var source = Source("NextSequenceValue");
        source.SequenceName = "dbo.OrderNumbers";
        step.ValueSources = [source];
        Fails(Check(step), "sequence ordering columns");
    }

    [Fact]
    public void ASequenceOrderingColumnMustHaveTheSequenceOrderRole()
    {
        var step = Step("InsertFromQuery");
        var source = Source("NextSequenceValue");
        source.SequenceName = "dbo.OrderNumbers";
        source.SequenceOrderColumns = [Column("Key", "Id")];
        step.ValueSources = [source];
        Fails(Check(step), "is not valid there");
    }

    [Fact]
    public void AConstantNeedsItsValueAndType()
    {
        var step = Step("BulkInsert");
        var source = Source("Constant");
        source.ConstantValue = "1";
        step.ValueSources = [source];
        Fails(Check(step), "a constant value and the type that reads it");
    }

    [Fact]
    public void AConstantTypeMustBeADataType()
    {
        var step = Step("BulkInsert");
        var source = Source("Constant");
        source.ConstantValue = "1";
        source.ConstantDataType = "Telepathy";
        step.ValueSources = [source];
        Fails(Check(step), "not a data type");
    }

    [Fact]
    public void ACurrentTimestampRefusesAConstant()
    {
        var step = Step("BulkInsert");
        var source = Source("CurrentTimestamp");
        source.ConstantValue = "1";
        step.ValueSources = [source];
        Fails(Check(step), "states a constant, which a CurrentTimestamp value source does not use");
    }

    [Fact]
    public void AValueSourceKindMustExist()
    {
        var step = Step("BulkInsert");
        step.ValueSources = [Source("Random")];
        Fails(Check(step), "not a value source kind");
    }

    [Fact]
    public void AValueSourceNeedsATargetColumn()
    {
        var step = Step("BulkInsert");
        step.ValueSources = [Source("CurrentTimestamp", " ")];
        Fails(Check(step), "the target column name");
    }

    [Fact]
    public void TwoValueSourcesCannotSupplyTheSameColumn()
    {
        var step = Step("BulkInsert");
        step.ValueSources = [Source("CurrentTimestamp", "LoadedAt"), Source("CurrentTimestamp", "loadedat")];
        Fails(Check(step), "by two value sources");
    }

    [Fact]
    public void AnExecuteStepTakesItsParametersAsConstantAndTimestampValueSources()
    {
        var step = Step("Execute");
        var constant = Source("Constant", "Origin");
        constant.ConstantValue = "import";
        constant.ConstantDataType = "String";
        step.ValueSources = [constant, Source("CurrentTimestamp", "LoadedAt")];
        Check(step).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AnExecuteStepRefusesASequenceParameter()
    {
        var step = Step("Execute");
        var sequence = Source("NextSequenceValue", "Number");
        sequence.SequenceName = "OrderNumbers";
        sequence.SequenceOrderColumns = [Column("SequenceOrder", "CreatedAt")];
        step.ValueSources = [sequence];
        Fails(Check(step), "cannot supply the parameter of an executable container");
    }
}
