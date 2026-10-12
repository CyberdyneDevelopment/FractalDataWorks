using Fdw.Results;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="PipelineLoadValidator"/> (FDW-1108): a load configuration that is incomplete or contradicts
/// itself is refused with a reason, and nothing is filled in on the caller's behalf. A pipeline states a load mode
/// (and load options for Upsert only) or load steps, never both.
/// </summary>
public sealed class PipelineLoadValidatorTests
{
    private static IGenericResult Check(
        string? loadMode,
        IList<PipelineLoadStepConfiguration>? loadSteps = null,
        IList<PipelineRunParameterConfiguration>? parameters = null) =>
        PipelineLoadValidator.Validate(loadMode, loadSteps, parameters, null, NullLogger.Instance);

    private static IGenericResult CheckSteps(params PipelineLoadStepConfiguration[] steps) => Check(null, steps);

    private static IGenericResult CheckOwnLoad(string mode, PipelineLoadStepConfiguration own) => Check(mode, [own]);

    private static PipelineLoadColumnConfiguration Column(string role, string column, int ordinal = 0) =>
        new() { Name = column, Role = role, ColumnName = column, Ordinal = ordinal };

    private static PipelineLoadStepConfiguration OwnLoad(params PipelineLoadColumnConfiguration[] columns) =>
        new() { Name = PipelineLoadStepConfiguration.PipelineLoadName, Phase = "Load", StepOrder = null, IsEnabled = true, Columns = columns };

    private static PipelineLoadStepConfiguration UpsertLoad() => OwnLoad(Column("Key", "Id"), Column("Write", "Name"));

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

    // ---- the implicit load: a load mode with or without load options

    [Theory]
    [InlineData("Append")]
    [InlineData("Replace")]
    public void AnAppendOrReplaceWithNoOptionsIsValid(string mode) => Check(mode).IsSuccess.ShouldBeTrue();

    [Fact]
    public void AnUpsertWithKeyAndWriteColumnsIsValid() => CheckOwnLoad("Upsert", UpsertLoad()).IsSuccess.ShouldBeTrue();

    [Fact]
    public void AnUpsertWithEveryOptionIsValid()
    {
        var load = OwnLoad(Column("Key", "Id"), Column("Compare", "Name"), Column("Write", "Name"));
        load.EffectiveFromColumn = "ValidFrom";
        load.EffectiveToColumn = "ValidTo";
        load.CurrentFlagColumn = "IsCurrent";
        load.InsertOnlyRuleColumn = "Source";
        load.InsertOnlyRuleValue = "Manual";
        load.SoftDeleteMode = "AbsentFromSource";
        load.DeletedFlagColumn = "IsDeleted";
        load.DeletedAtColumn = "DeletedAt";
        CheckOwnLoad("Upsert", load).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ALoadWithNoStepsMustStateALoadMode(string? mode) => Fails(Check(mode), "must state a load mode");

    [Fact]
    public void AnUnknownLoadModeIsRefused() => Fails(Check("Merge"), "Load mode 'Merge'");

    [Fact]
    public void AnUpsertWithNoOptionsIsRefused() => Fails(Check("Upsert"), "needs load options");

    [Fact]
    public void AnUpsertWithoutKeyColumnsIsRefused() =>
        Fails(CheckOwnLoad("Upsert", OwnLoad(Column("Write", "Name"))), "key columns is needed");

    [Fact]
    public void AnUpsertWithoutWriteColumnsIsRefused() =>
        Fails(CheckOwnLoad("Upsert", OwnLoad(Column("Key", "Id"))), "write columns is needed");

    [Theory]
    [InlineData("Key")]
    [InlineData("Compare")]
    [InlineData("Write")]
    public void AnAppendRefusesKeyCompareAndWriteColumns(string role) =>
        Fails(CheckOwnLoad("Append", OwnLoad(Column(role, "Name"))), "which Append does not use");

    [Fact]
    public void AReplaceRefusesVersioning()
    {
        var load = OwnLoad();
        load.EffectiveFromColumn = "ValidFrom";
        load.EffectiveToColumn = "ValidTo";
        Fails(CheckOwnLoad("Replace", load), "versioning is stated, which Replace does not use");
    }

    [Fact]
    public void AnAppendRefusesASoftDelete()
    {
        var load = OwnLoad();
        load.SoftDeleteMode = "AbsentFromSource";
        load.DeletedFlagColumn = "IsDeleted";
        Fails(CheckOwnLoad("Append", load), "a soft delete is stated, which Append does not use");
    }

    [Theory]
    [InlineData("Constant")]
    [InlineData("CurrentTimestamp")]
    public void AnyLoadModeAllowsTopLevelConstantAndTimestampValueSources(string kind)
    {
        var load = OwnLoad();
        var source = Source(kind);
        source.ConstantValue = kind == "Constant" ? "1" : null;
        source.ConstantDataType = kind == "Constant" ? "Int32" : null;
        load.ValueSources = [source];
        CheckOwnLoad("Append", load).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void TopLevelValueSourcesRefuseAColumnMapping()
    {
        var load = OwnLoad();
        var source = Source("Column");
        source.SourceField = "Name";
        load.ValueSources = [source];
        Fails(CheckOwnLoad("Append", load), "takes a source data set");
    }

    [Fact]
    public void TheOwnLoadCannotStateAStepOnlyOption()
    {
        var load = OwnLoad();
        load.Kind = "BulkInsert";
        Fails(CheckOwnLoad("Append", load), "which only a load step has");
    }

    [Fact]
    public void ThePipelineCannotStateItsOwnLoadTwice() =>
        Fails(Check("Append", [OwnLoad(), OwnLoad()]), "more than once");

    // ---- load steps replace the load mode

    [Fact]
    public void LoadStepsWithALoadModeAreRefused() =>
        Fails(Check("Append", [Step("BulkInsert")]), "stated together with load steps");

    [Fact]
    public void LoadStepsWithTheOwnLoadRowAreRefused() =>
        Fails(CheckSteps(OwnLoad(), Step("BulkInsert")), "together with load steps");

    [Theory]
    [InlineData("BulkInsert")]
    [InlineData("BulkUpsert")]
    [InlineData("InsertFromQuery")]
    [InlineData("Update")]
    [InlineData("Delete")]
    [InlineData("Truncate")]
    [InlineData("Execute")]
    public void EveryKindIsValidWithItsOwnOptions(string kind) => CheckSteps(Step(kind)).IsSuccess.ShouldBeTrue();

    [Fact]
    public void StepsMayBeListedOutOfOrderWhenTheirOrdersAreOneToN() =>
        CheckSteps(Step("Truncate", 2, "second"), Step("BulkInsert", 1, "first")).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void StepOrdersMustRunFromOneToTheNumberOfSteps(int order) =>
        Fails(CheckSteps(Step("Truncate", order)), "step orders run from 1");

    [Fact]
    public void TwoStepsWithTheSameOrderAreRefused() =>
        Fails(CheckSteps(Step("Truncate", 1, "a"), Step("Truncate", 1, "b")), "step order 1");

    [Fact]
    public void AStepMustHaveAName() => Fails(CheckSteps(Step("Truncate", 1, " ")), "has no name");

    [Fact]
    public void StepNamesAreUniqueIgnoringCase() =>
        Fails(CheckSteps(Step("Truncate", 1, "load"), Step("Truncate", 2, "LOAD")), "named 'LOAD'");

    [Fact]
    public void AStepMustNameItsKind()
    {
        var step = Step("Truncate");
        step.Kind = null;
        Fails(CheckSteps(step), "kind of step is not stated");
    }

    [Fact]
    public void AStepWithAnUnknownKindIsRefused() => Fails(CheckSteps(Step("Teleport")), "'Teleport' is not a load step kind");

    [Fact]
    public void AStepMustNameItsDestinationDataSet()
    {
        var step = Step("Truncate");
        step.DestinationDataSet = null;
        Fails(CheckSteps(step), "destination data set is not stated");
    }

    // ---- what each kind takes

    [Fact]
    public void ATruncateRefusesWriteColumns()
    {
        var step = Step("Truncate");
        step.Columns = [Column("Write", "Name")];
        Fails(CheckSteps(step), "which Truncate does not use");
    }

    [Fact]
    public void ADeleteRefusesWriteColumns()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", "Id"), Column("Write", "Name")];
        Fails(CheckSteps(step), "which Delete does not use");
    }

    [Fact]
    public void ABulkInsertRefusesKeyColumns()
    {
        var step = Step("BulkInsert");
        step.Columns = [Column("Key", "Id"), Column("Write", "Name")];
        Fails(CheckSteps(step), "which BulkInsert does not use");
    }

    [Fact]
    public void AnUpdateNeedsKeyColumns()
    {
        var step = Step("Update");
        step.Columns = [Column("Write", "Name")];
        Fails(CheckSteps(step), "key columns is needed");
    }

    [Fact]
    public void AnUpdateRefusesVersioning()
    {
        var step = Step("Update");
        step.EffectiveFromColumn = "ValidFrom";
        step.EffectiveToColumn = "ValidTo";
        Fails(CheckSteps(step), "versioning is stated, which Update does not use");
    }

    [Fact]
    public void ADeleteMayBeASoftDeleteByMarker()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "MarkerMatches";
        step.DeletedFlagColumn = "IsDeleted";
        step.SoftDeleteMarkerColumn = "Status";
        step.SoftDeleteMarkerValue = "Closed";
        CheckSteps(step).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ABulkInsertRefusesASoftDelete()
    {
        var step = Step("BulkInsert");
        step.SoftDeleteMode = "AbsentFromSource";
        step.DeletedFlagColumn = "IsDeleted";
        Fails(CheckSteps(step), "which BulkInsert does not use");
    }

    [Fact]
    public void AnInsertFromQueryNeedsASourceDataSet()
    {
        var step = Step("InsertFromQuery");
        step.SourceDataSet = null;
        Fails(CheckSteps(step), "a source data set is needed");
    }

    [Fact]
    public void AnInsertFromQueryMayFilterItsSource()
    {
        var step = Step("InsertFromQuery");
        step.FilterExpression = "Amount > {{minimum}}";
        CheckSteps(step).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ABulkInsertRefusesASourceDataSet()
    {
        var step = Step("BulkInsert");
        step.SourceDataSet = "Staging";
        Fails(CheckSteps(step), "a source data set is stated, which BulkInsert does not use");
    }

    [Fact]
    public void ABulkInsertRefusesAFilterExpression()
    {
        var step = Step("BulkInsert");
        step.FilterExpression = "Amount > 0";
        Fails(CheckSteps(step), "a filter expression is stated");
    }

    [Fact]
    public void AnExecuteNeedsItsContainer()
    {
        var step = Step("Execute");
        step.DestinationContainerName = null;
        Fails(CheckSteps(step), "a destination container name is needed");
    }

    [Fact]
    public void AnExecuteNeedsItsDataStore()
    {
        var step = Step("Execute");
        step.DestinationDataStoreName = null;
        Fails(CheckSteps(step), "a destination data store is needed");
    }

    [Fact]
    public void AnExecuteNeedsTheContainersPathToo()
    {
        var step = Step("Execute");
        step.DestinationPathName = null;
        Fails(CheckSteps(step), "a destination container path is needed");
    }

    [Fact]
    public void ABulkUpsertRefusesAContainer()
    {
        var step = Step("BulkUpsert");
        step.DestinationPathName = "dbo";
        step.DestinationContainerName = "LoadSummary";
        Fails(CheckSteps(step), "a destination container path is stated, which BulkUpsert does not use");
    }

    [Fact]
    public void ABulkUpsertRefusesADataStore()
    {
        var step = Step("BulkUpsert");
        step.DestinationDataStoreName = "Main";
        Fails(CheckSteps(step), "a destination data store is stated, which BulkUpsert does not use");
    }

    // ---- column lists

    [Fact]
    public void AColumnWithAnUnknownRoleIsRefused()
    {
        var step = Step("Delete");
        step.Columns = [Column("Index", "Id")];
        Fails(CheckSteps(step), "not a load column role");
    }

    [Fact]
    public void ASequenceOrderColumnDoesNotBelongOnAStep()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", "Id"), Column("SequenceOrder", "CreatedAt")];
        Fails(CheckSteps(step), "is not valid there");
    }

    [Fact]
    public void AColumnNeedsAName()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", " ")];
        Fails(CheckSteps(step), "has no column name");
    }

    [Fact]
    public void TwoColumnsOfOneRoleCannotShareAnOrdinal()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", "Id", 0), Column("Key", "Region", 0)];
        Fails(CheckSteps(step), "at ordinal 0");
    }

    [Fact]
    public void ACompositeKeyWithDistinctOrdinalsIsValid()
    {
        var step = Step("Delete");
        step.Columns = [Column("Key", "Id", 0), Column("Key", "Region", 1)];
        CheckSteps(step).IsSuccess.ShouldBeTrue();
    }

    // ---- versioning, insert-only rule, soft delete

    [Fact]
    public void VersioningNeedsBothColumns()
    {
        var load = UpsertLoad();
        load.EffectiveFromColumn = "ValidFrom";
        Fails(CheckOwnLoad("Upsert", load), "EffectiveToColumn");
    }

    [Fact]
    public void ACurrentFlagColumnNeedsVersioning()
    {
        var load = UpsertLoad();
        load.CurrentFlagColumn = "IsCurrent";
        Fails(CheckOwnLoad("Upsert", load), "EffectiveFromColumn");
    }

    [Fact]
    public void AnInsertOnlyRuleNeedsVersioning()
    {
        var load = UpsertLoad();
        load.InsertOnlyRuleColumn = "Source";
        load.InsertOnlyRuleValue = "Manual";
        Fails(CheckOwnLoad("Upsert", load), "EffectiveFromColumn");
    }

    [Fact]
    public void AnInsertOnlyRuleNeedsItsValue()
    {
        var load = UpsertLoad();
        load.EffectiveFromColumn = "ValidFrom";
        load.EffectiveToColumn = "ValidTo";
        load.InsertOnlyRuleColumn = "Source";
        Fails(CheckOwnLoad("Upsert", load), "InsertOnlyRuleValue");
    }

    [Fact]
    public void ASoftDeleteFlagWithoutAModeIsRefused()
    {
        var step = Step("Delete");
        step.DeletedFlagColumn = "IsDeleted";
        Fails(CheckSteps(step), "names its mode");
    }

    [Fact]
    public void ASoftDeleteModeThatDoesNotExistIsRefused()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "Never";
        step.DeletedFlagColumn = "IsDeleted";
        Fails(CheckSteps(step), "not a soft delete mode");
    }

    [Fact]
    public void ASoftDeleteNeedsItsFlagColumn()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "AbsentFromSource";
        Fails(CheckSteps(step), "names its flag column");
    }

    [Fact]
    public void AMarkerMatchesSoftDeleteNeedsItsMarker()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "MarkerMatches";
        step.DeletedFlagColumn = "IsDeleted";
        Fails(CheckSteps(step), "needs a marker column and a marker value");
    }

    [Fact]
    public void AnAbsentFromSourceSoftDeleteRefusesAMarker()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "AbsentFromSource";
        step.DeletedFlagColumn = "IsDeleted";
        step.SoftDeleteMarkerColumn = "Status";
        step.SoftDeleteMarkerValue = "Closed";
        Fails(CheckSteps(step), "does not use a marker");
    }

    [Fact]
    public void ASoftDeleteMarkerNeedsBothParts()
    {
        var step = Step("Delete");
        step.SoftDeleteMode = "MarkerMatches";
        step.DeletedFlagColumn = "IsDeleted";
        step.SoftDeleteMarkerColumn = "Status";
        Fails(CheckSteps(step), "SoftDeleteMarkerValue");
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
        CheckSteps(step).IsSuccess.ShouldBeTrue();
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
        CheckSteps(step).IsSuccess.ShouldBeTrue();
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
        Fails(CheckSteps(step), "a sequence schema");
    }

    [Fact]
    public void ASequenceDatabaseOnATimestampIsRefused()
    {
        var step = Step("BulkInsert");
        var source = Source("CurrentTimestamp");
        source.SequenceDatabase = "Sales";
        step.ValueSources = [source];
        Fails(CheckSteps(step), "a sequence database");
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
        Fails(CheckSteps(step), "takes a source data set");
    }

    [Fact]
    public void AColumnValueSourceNeedsItsSourceField()
    {
        var step = Step("InsertFromQuery");
        step.ValueSources = [Source("Column")];
        Fails(CheckSteps(step), "a source field");
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
        Fails(CheckSteps(step), "a source field");
    }

    [Fact]
    public void ANextSequenceValueNeedsASequenceName()
    {
        var step = Step("InsertFromQuery");
        var source = Source("NextSequenceValue");
        source.SequenceOrderColumns = [Column("SequenceOrder", "CreatedAt")];
        step.ValueSources = [source];
        Fails(CheckSteps(step), "a sequence name");
    }

    [Fact]
    public void ANextSequenceValueNeedsOrderingColumns()
    {
        var step = Step("InsertFromQuery");
        var source = Source("NextSequenceValue");
        source.SequenceName = "dbo.OrderNumbers";
        step.ValueSources = [source];
        Fails(CheckSteps(step), "sequence ordering columns");
    }

    [Fact]
    public void ASequenceOrderingColumnMustHaveTheSequenceOrderRole()
    {
        var step = Step("InsertFromQuery");
        var source = Source("NextSequenceValue");
        source.SequenceName = "dbo.OrderNumbers";
        source.SequenceOrderColumns = [Column("Key", "Id")];
        step.ValueSources = [source];
        Fails(CheckSteps(step), "is not valid there");
    }

    [Fact]
    public void AConstantNeedsItsValueAndType()
    {
        var step = Step("BulkInsert");
        var source = Source("Constant");
        source.ConstantValue = "1";
        step.ValueSources = [source];
        Fails(CheckSteps(step), "a constant value and the type that reads it");
    }

    [Fact]
    public void AConstantTypeMustBeADataType()
    {
        var step = Step("BulkInsert");
        var source = Source("Constant");
        source.ConstantValue = "1";
        source.ConstantDataType = "Telepathy";
        step.ValueSources = [source];
        Fails(CheckSteps(step), "not a data type");
    }

    [Fact]
    public void ACurrentTimestampRefusesAConstant()
    {
        var step = Step("BulkInsert");
        var source = Source("CurrentTimestamp");
        source.ConstantValue = "1";
        step.ValueSources = [source];
        Fails(CheckSteps(step), "states a constant, which a CurrentTimestamp value source does not use");
    }

    [Fact]
    public void AValueSourceKindMustExist()
    {
        var step = Step("BulkInsert");
        step.ValueSources = [Source("Random")];
        Fails(CheckSteps(step), "not a value source kind");
    }

    [Fact]
    public void AValueSourceNeedsATargetColumn()
    {
        var step = Step("BulkInsert");
        step.ValueSources = [Source("CurrentTimestamp", " ")];
        Fails(CheckSteps(step), "the target column name");
    }

    [Fact]
    public void TwoValueSourcesCannotSupplyTheSameColumn()
    {
        var step = Step("BulkInsert");
        step.ValueSources = [Source("CurrentTimestamp", "LoadedAt"), Source("CurrentTimestamp", "loadedat")];
        Fails(CheckSteps(step), "by two value sources");
    }

    [Fact]
    public void AnExecuteStepTakesItsParametersAsConstantAndTimestampValueSources()
    {
        var step = Step("Execute");
        var constant = Source("Constant", "Origin");
        constant.ConstantValue = "import";
        constant.ConstantDataType = "String";
        step.ValueSources = [constant, Source("CurrentTimestamp", "LoadedAt")];
        CheckSteps(step).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AnExecuteStepRefusesASequenceParameter()
    {
        var step = Step("Execute");
        var sequence = Source("NextSequenceValue", "Number");
        sequence.SequenceName = "OrderNumbers";
        sequence.SequenceOrderColumns = [Column("SequenceOrder", "CreatedAt")];
        step.ValueSources = [sequence];
        Fails(CheckSteps(step), "cannot supply the parameter of an executable container");
    }

    // ---- run parameters

    [Theory]
    [InlineData("String")]
    [InlineData("Int32")]
    [InlineData("Int64")]
    [InlineData("Decimal")]
    [InlineData("Double")]
    [InlineData("DateTimeOffset")]
    [InlineData("DateTime")]
    [InlineData("Date")]
    [InlineData("Guid")]
    [InlineData("Bool")]
    public void ARunParameterOfEveryDataTypeThatFitsAnExpressionIsValid(string type) =>
        Check("Append", null, [new PipelineRunParameterConfiguration { Name = "since", ParameterType = type, IsRequired = true }]).IsSuccess.ShouldBeTrue();

    [Fact]
    public void ARunParameterNeedsAName() =>
        Fails(Check("Append", null, [new PipelineRunParameterConfiguration { Name = " ", ParameterType = "String" }]), "has no name");

    [Fact]
    public void ARunParameterIsDeclaredOnce() =>
        Fails(
            Check("Append", null, [
                new PipelineRunParameterConfiguration { Name = "since", ParameterType = "String" },
                new PipelineRunParameterConfiguration { Name = "SINCE", ParameterType = "Int32" }]),
            "declared twice");

    [Theory]
    [InlineData("Telepathy")]
    [InlineData("Binary")]
    [InlineData("")]
    public void ARunParameterTypeMustBeADataTypeThatFitsAnExpression(string type) =>
        Fails(Check("Append", null, [new PipelineRunParameterConfiguration { Name = "since", ParameterType = type }]), "not a data type a run parameter can have");

    [Fact]
    public void RunParametersAreCheckedWhenTheLoadIsSteps() =>
        Fails(
            Check(null, [Step("Truncate")], [new PipelineRunParameterConfiguration { Name = "since", ParameterType = "Binary" }]),
            "not a data type a run parameter can have");

    // ---- the pipeline states nothing by default

    [Fact]
    public void ABatchCopyPipelineStatesNoLoadModeStepsOrParametersUntilSomeoneDoes()
    {
        // Act
        var pipeline = new BatchCopyPipelineConfiguration();

        // Assert
        pipeline.LoadMode.ShouldBeNull();
        pipeline.LoadSteps.ShouldBeNull();
        pipeline.RunParameters.ShouldBeNull();
    }
}
