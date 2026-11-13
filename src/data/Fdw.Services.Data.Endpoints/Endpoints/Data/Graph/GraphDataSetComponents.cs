using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads a graph pipeline's stored component into the Graph data set's own (FDW-5287): the payload of a graph is read once, by the one reader a
/// graph pipeline uses, and what it yields is copied here member for member into the classes of <c>data.GraphDataSet*</c>.
/// </summary>
/// <remarks>
/// A Graph data set has no member for what only a pipeline's component takes: a destination's or an action's settings, a loop's parent, the run
/// stage a component runs in, a binding to a run parameter. A component that states one is not stored with the setting dropped;
/// <see cref="UnsupportedSettings"/> names each so the caller refuses it.
/// </remarks>
public static class GraphDataSetComponents
{
    // What a pipeline's component can state and a Graph data set's has no member for: one row each, so no setting is a branch of its own.
    private static readonly (string Name, Func<PipelineComponentConfiguration, bool> IsStated)[] Unsupported =
    [
        ("parentComponent", source => source.ParentComponent is not null),
        ("splitField", source => source.SplitField is not null),
        ("loadKind", source => source.LoadKind is not null),
        ("truncateFirst", source => source.TruncateFirst is not null),
        ("variableName", source => source.VariableName is not null),
        ("startValue", source => source.StartValue is not null),
        ("endValue", source => source.EndValue is not null),
        ("stepValue", source => source.StepValue is not null),
        ("listVariable", source => source.ListVariable is not null),
        ("conditionExpression", source => source.ConditionExpression is not null),
        ("maxIterations", source => source.MaxIterations is not null),
        ("constantType", source => source.ConstantType is not null),
        ("constantText", source => source.ConstantText is not null),
        ("targetPipeline", source => source.TargetPipeline is not null),
        ("awaitCompletion", source => source.AwaitCompletion is not null),
        ("resultVariable", source => source.ResultVariable is not null),
        ("failOnChildFailure", source => source.FailOnChildFailure is not null),
        ("notificationRuleName", source => source.NotificationRuleName is not null),
        ("runPoint", source => source.RunPoint is not null),
        ("notificationMessage", source => source.NotificationMessage is not null),
        ("transformationName", source => source.TransformationName is not null),
        ("stage", source => source.Stage is not null),
        ("effectiveFromColumn", source => source.EffectiveFromColumn is not null),
        ("effectiveToColumn", source => source.EffectiveToColumn is not null),
        ("currentFlagColumn", source => source.CurrentFlagColumn is not null),
        ("insertOnlyRuleColumn", source => source.InsertOnlyRuleColumn is not null),
        ("insertOnlyRuleValue", source => source.InsertOnlyRuleValue is not null),
        ("softDeleteMode", source => source.SoftDeleteMode is not null),
        ("deletedFlagColumn", source => source.DeletedFlagColumn is not null),
        ("deletedFlagValue", source => source.DeletedFlagValue is not null),
        ("deletedAtColumn", source => source.DeletedAtColumn is not null),
        ("softDeleteMarkerColumn", source => source.SoftDeleteMarkerColumn is not null),
        ("softDeleteMarkerValue", source => source.SoftDeleteMarkerValue is not null),
        ("stageContainer", source => source.StageContainer is not null),
        ("stageKeyColumn", source => source.StageKeyColumn is not null),
        ("sweepRejectedRowsRule", source => source.SweepRejectedRowsRule is not null),
        ("unmatchedKeysRule", source => source.UnmatchedKeysRule is not null),
        ("valueSources", source => source.ValueSources.Count > 0),
        ("settingBindings", source => source.SettingBindings.Count > 0),
        ("parameters", source => source.Parameters.Count > 0),
    ];

    /// <summary>Copies a graph pipeline's component into a Graph data set's component, with its list settings and its stage.</summary>
    /// <param name="source">The component as the graph pipeline reader produced it.</param>
    /// <returns>The component of the Graph data set.</returns>
    public static GraphDataSetComponentConfiguration Read(PipelineComponentConfiguration source) => Component(source);

    /// <summary>Lists the settings a component states that a Graph data set's component has no member for.</summary>
    /// <param name="source">The component as the graph pipeline reader produced it.</param>
    /// <returns>The camel-cased setting names, each once; empty when nothing is lost.</returns>
    public static IReadOnlyList<string> UnsupportedSettings(PipelineComponentConfiguration source) =>
        [.. Unsupported.Where(setting => setting.IsStated(source)).Select(setting => setting.Name)];

    private static GraphDataSetComponentConfiguration Component(PipelineComponentConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            ComponentType = source.ComponentType,
            DataSetName = source.DataSetName,
            DataStoreName = source.DataStoreName,
            PathName = source.PathName,
            ContainerName = source.ContainerName,
            PhysicalPath = source.PhysicalPath,
            PhysicalName = source.PhysicalName,
            TargetKind = source.TargetKind,
            FilesList = source.FilesList,
            BatchSize = source.BatchSize,
            JoinKind = source.JoinKind,
            RightPrefix = source.RightPrefix,
            Routing = source.Routing,
            UnmatchedOutput = source.UnmatchedOutput,
            ErrorOutput = source.ErrorOutput,
            FieldName = source.FieldName,
            TargetName = source.TargetName,
            Delimiter = source.Delimiter,
            KeepsEmpty = source.KeepsEmpty,
            ScriptText = source.ScriptText,
            MaxRows = source.MaxRows,
            LayoutX = source.LayoutX,
            LayoutY = source.LayoutY,
            Fields = [.. source.Fields.Select(ComponentField)],
            Columns = [.. source.Columns.Select(ComponentColumn)],
            SplitOutputs = [.. source.SplitOutputs.Select(SplitOutput)],
            Stages = [.. source.Stages.Select(Stage)],
        };

    private static GraphDataSetComponentFieldConfiguration ComponentField(PipelineComponentFieldConfiguration source) => new()
        {
            Id = source.Id,
            SettingName = source.SettingName,
            InputName = source.InputName,
            Ordinal = source.Ordinal,
            TargetField = source.TargetField,
            SourceField = source.SourceField,
            ConstantType = source.ConstantType,
            ConstantText = source.ConstantText,
            TargetType = source.TargetType,
            DefaultText = source.DefaultText,
            IsRequired = source.IsRequired,
            TransformationChain = [.. source.TransformationChain.Select(FieldTransformation)],
        };

    private static GraphDataSetComponentFieldTransformationConfiguration FieldTransformation(PipelineComponentFieldTransformationConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            TransformationId = source.TransformationId,
            Ordinal = source.Ordinal,
        };

    private static GraphDataSetComponentColumnConfiguration ComponentColumn(PipelineComponentColumnConfiguration source) => new()
        {
            Id = source.Id,
            SettingName = source.SettingName,
            Ordinal = source.Ordinal,
            ColumnName = source.ColumnName,
        };

    private static GraphDataSetComponentSplitOutputConfiguration SplitOutput(PipelineComponentSplitOutputConfiguration source) => new()
        {
            Id = source.Id,
            Ordinal = source.Ordinal,
            OutputName = source.OutputName,
            FilterExpression = source.FilterExpression,
        };

    private static GraphDataSetStageConfiguration Stage(PipelineTransformConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            OperationType = source.OperationType,
            ExecutionOrder = source.ExecutionOrder,
            IsEnabled = source.IsEnabled,
            FilterExpression = source.FilterExpression,
            GroupByFields = [.. source.GroupByFields.Select(StageGroupByField)],
            SortFields = [.. source.SortFields.Select(StageSortField)],
            OrderField = source.OrderField,
            KeepRule = source.KeepRule,
            Aggregations = [.. source.Aggregations.Select(StageAggregation)],
            Calculations = [.. source.Calculations.Select(StageCalculation)],
            Lookups = [.. source.Lookups.Select(StageLookup)],
            OnFail = source.OnFail,
            Checks = [.. source.Checks.Select(StageCheck)],
            ComputeTransformationId = source.ComputeTransformationId,
            ComputeInputs = [.. source.ComputeInputs.Select(StageComputeInput)],
            ComputeTables = [.. source.ComputeTables.Select(StageComputeTable)],
            ComputeOutputs = [.. source.ComputeOutputs.Select(StageComputeOutput)],
            ComputeScalars = [.. source.ComputeScalars.Select(StageComputeScalar)],
            ComputeOnRowFailure = source.ComputeOnRowFailure,
        };

    private static GraphDataSetStageAggregationConfiguration StageAggregation(PipelineTransformAggregationConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            SourceField = source.SourceField,
            AggregateFunction = source.AggregateFunction,
            OutputField = source.OutputField,
            ExecutionOrder = source.ExecutionOrder,
        };

    private static GraphDataSetStageCalculationConfiguration StageCalculation(PipelineTransformCalculationConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            OutputField = source.OutputField,
            Expression = source.Expression,
            ExecutionOrder = source.ExecutionOrder,
        };

    private static GraphDataSetStageCheckConfiguration StageCheck(PipelineTransformCheckConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            Expression = source.Expression,
            Ordinal = source.Ordinal,
        };

    private static GraphDataSetStageComputeInputConfiguration StageComputeInput(PipelineTransformComputeInputConfiguration source) => new()
        {
            Id = source.Id,
            InputName = source.InputName,
            SourceField = source.SourceField,
        };

    private static GraphDataSetStageComputeOutputConfiguration StageComputeOutput(PipelineTransformComputeOutputConfiguration source) => new()
        {
            Id = source.Id,
            OutputName = source.OutputName,
            Route = source.Route,
            RouteName = source.RouteName,
        };

    private static GraphDataSetStageComputeScalarConfiguration StageComputeScalar(PipelineTransformComputeScalarConfiguration source) => new()
        {
            Id = source.Id,
            InputName = source.InputName,
            ValueText = source.ValueText,
            ValueType = source.ValueType,
        };

    private static GraphDataSetStageComputeTableConfiguration StageComputeTable(PipelineTransformComputeTableConfiguration source) => new()
        {
            Id = source.Id,
            TableName = source.TableName,
            FromKind = source.FromKind,
            FromName = source.FromName,
        };

    private static GraphDataSetStageGroupByFieldConfiguration StageGroupByField(PipelineTransformGroupByFieldConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            FieldName = source.FieldName,
            Ordinal = source.Ordinal,
        };

    private static GraphDataSetStageLookupConfiguration StageLookup(PipelineTransformLookupConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            LookupDataSet = source.LookupDataSet,
            LookupDataStoreName = source.LookupDataStoreName,
            LookupDataStorePath = source.LookupDataStorePath,
            LookupContainerName = source.LookupContainerName,
            LookupKeyField = source.LookupKeyField,
            SourceKeyField = source.SourceKeyField,
            OutputFieldPrefix = source.OutputFieldPrefix,
            LookupValueField = source.LookupValueField,
            JoinType = source.JoinType,
        };

    private static GraphDataSetStageSortFieldConfiguration StageSortField(PipelineTransformSortFieldConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            FieldName = source.FieldName,
            Direction = source.Direction,
            Ordinal = source.Ordinal,
        };
}
