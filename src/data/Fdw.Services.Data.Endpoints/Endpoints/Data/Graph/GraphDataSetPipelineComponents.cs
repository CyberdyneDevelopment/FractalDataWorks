using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads a Graph data set's stored component back into the graph pipeline's component (FDW-5295): the mirror image of <see cref="GraphDataSetComponents"/>,
/// so the pipeline's own writer states the component and no second writer of the shape exists.
/// </summary>
/// <remarks>
/// A Graph data set has no member for what only a pipeline's component takes (see <see cref="GraphDataSetComponents.UnsupportedSettings"/>), and a save refuses
/// each, so nothing is lost here and nothing is stated that was not stored.
/// </remarks>
internal static class GraphDataSetPipelineComponents
{
    /// <summary>Copies a stored component of a Graph data set into the graph pipeline's component, with its list settings and its stage.</summary>
    /// <param name="source">The stored component.</param>
    /// <returns>The component of the graph pipeline's stored configuration.</returns>
    internal static PipelineComponentConfiguration Write(GraphDataSetComponentConfiguration source) => new()
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

    private static PipelineComponentFieldConfiguration ComponentField(GraphDataSetComponentFieldConfiguration source) => new()
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

    private static PipelineComponentFieldTransformationConfiguration FieldTransformation(GraphDataSetComponentFieldTransformationConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            TransformationId = source.TransformationId,
            Ordinal = source.Ordinal,
        };

    private static PipelineComponentColumnConfiguration ComponentColumn(GraphDataSetComponentColumnConfiguration source) => new()
        {
            Id = source.Id,
            SettingName = source.SettingName,
            Ordinal = source.Ordinal,
            ColumnName = source.ColumnName,
        };

    private static PipelineComponentSplitOutputConfiguration SplitOutput(GraphDataSetComponentSplitOutputConfiguration source) => new()
        {
            Id = source.Id,
            Ordinal = source.Ordinal,
            OutputName = source.OutputName,
            FilterExpression = source.FilterExpression,
        };

    private static PipelineTransformConfiguration Stage(GraphDataSetStageConfiguration source) => new()
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

    private static PipelineTransformAggregationConfiguration StageAggregation(GraphDataSetStageAggregationConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            SourceField = source.SourceField,
            AggregateFunction = source.AggregateFunction,
            OutputField = source.OutputField,
            ExecutionOrder = source.ExecutionOrder,
        };

    private static PipelineTransformCalculationConfiguration StageCalculation(GraphDataSetStageCalculationConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            OutputField = source.OutputField,
            Expression = source.Expression,
            ExecutionOrder = source.ExecutionOrder,
        };

    private static PipelineTransformCheckConfiguration StageCheck(GraphDataSetStageCheckConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            Expression = source.Expression,
            Ordinal = source.Ordinal,
        };

    private static PipelineTransformComputeInputConfiguration StageComputeInput(GraphDataSetStageComputeInputConfiguration source) => new()
        {
            Id = source.Id,
            InputName = source.InputName,
            SourceField = source.SourceField,
        };

    private static PipelineTransformComputeOutputConfiguration StageComputeOutput(GraphDataSetStageComputeOutputConfiguration source) => new()
        {
            Id = source.Id,
            OutputName = source.OutputName,
            Route = source.Route,
            RouteName = source.RouteName,
        };

    private static PipelineTransformComputeScalarConfiguration StageComputeScalar(GraphDataSetStageComputeScalarConfiguration source) => new()
        {
            Id = source.Id,
            InputName = source.InputName,
            ValueText = source.ValueText,
            ValueType = source.ValueType,
        };

    private static PipelineTransformComputeTableConfiguration StageComputeTable(GraphDataSetStageComputeTableConfiguration source) => new()
        {
            Id = source.Id,
            TableName = source.TableName,
            FromKind = source.FromKind,
            FromName = source.FromName,
        };

    private static PipelineTransformGroupByFieldConfiguration StageGroupByField(GraphDataSetStageGroupByFieldConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            FieldName = source.FieldName,
            Ordinal = source.Ordinal,
        };

    private static PipelineTransformLookupConfiguration StageLookup(GraphDataSetStageLookupConfiguration source) => new()
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

    private static PipelineTransformSortFieldConfiguration StageSortField(GraphDataSetStageSortFieldConfiguration source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            FieldName = source.FieldName,
            Direction = source.Direction,
            Ordinal = source.Ordinal,
        };
}
