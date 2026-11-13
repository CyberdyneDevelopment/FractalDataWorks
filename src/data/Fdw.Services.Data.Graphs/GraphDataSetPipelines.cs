using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Data.Graphs;

/// <summary>
/// Reads the stored graph of a Graph data set as the graph the engine runs (FDW-5291): member for member, the opposite direction of the copy that
/// stores a graph pipeline's components as <c>data.GraphDataSet*</c> rows (<c>GraphDataSetComponents</c>, FDW-5287).
/// </summary>
/// <remarks>
/// A Graph data set has no member for what only a pipeline's component takes (a destination's or action's settings, a loop's parent, a run stage, a
/// binding to a run parameter), so none is set here: the component is a top-level component with a stage of none. The pipeline's own settings the
/// engine needs (the link capacity) are stated by the caller, never made up here.
/// </remarks>
public static class GraphDataSetPipelines
{
    /// <summary>Reads a Graph data set's stored graph.</summary>
    /// <param name="dataSet">The Graph data set with its components and links.</param>
    /// <param name="linkCapacity">The batches a link buffers before a writer waits.</param>
    /// <returns>The graph, named for the data set, with no destination.</returns>
    public static GraphPipelineConfiguration Read(IGraphDataSetConfiguration dataSet, int linkCapacity) => new()
    {
        Id = dataSet.Id,
        Name = dataSet.Name,
        IsEnabled = true,
        LinkCapacity = linkCapacity,
        Components = [.. dataSet.Components.Select(Component)],
        Links = [.. dataSet.Links.Select(Link)],
    };

    /// <summary>Keeps a component and everything upstream of it, so a preview at that component runs only what it needs (FDW-5454).</summary>
    /// <remarks>
    /// A component that does not feed it, and the lines among those, are left out. An output of a kept component whose only lines went to a left-out component
    /// is stated as dropped, so the engine's own check that every output is linked or dropped still holds; the end's own output is not, as the read consumes it.
    /// </remarks>
    /// <param name="graph">The graph read from the draft.</param>
    /// <param name="end">The component and output the rows are read at.</param>
    /// <returns>The graph of that component and what feeds it.</returns>
    public static GraphPipelineConfiguration UpTo(GraphPipelineConfiguration graph, GraphEnd end)
    {
        var kept = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { end.Component };
        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var link in graph.Links.Where(candidate => kept.Contains(candidate.ToComponent)))
            {
                grew |= kept.Add(link.FromComponent);
            }
        }

        var links = graph.Links.Where(link => kept.Contains(link.FromComponent) && kept.Contains(link.ToComponent)).ToList();
        return new GraphPipelineConfiguration
        {
            Id = graph.Id,
            Name = graph.Name,
            IsEnabled = graph.IsEnabled,
            LinkCapacity = graph.LinkCapacity,
            Components = [.. graph.Components.Where(component => kept.Contains(component.Name))],
            Links = links,
            DroppedOutputs = DroppedBy(graph.Links.Where(link => kept.Contains(link.FromComponent) && !kept.Contains(link.ToComponent)), links, end),
        };
    }

    // The outputs a kept component sent to a left-out component and sends nowhere else, each once.
    private static List<PipelineDroppedOutputConfiguration> DroppedBy(IEnumerable<PipelineLinkConfiguration> removed, List<PipelineLinkConfiguration> kept, GraphEnd end)
    {
        var dropped = new List<PipelineDroppedOutputConfiguration>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var link in removed.Where(candidate => !Leaves(kept, candidate.FromComponent, candidate.FromOutput) && !IsEnd(end, candidate.FromComponent, candidate.FromOutput)))
        {
            if (seen.Add($"{link.FromComponent}/{link.FromOutput}"))
            {
                dropped.Add(new PipelineDroppedOutputConfiguration { ComponentName = link.FromComponent, OutputName = link.FromOutput });
            }
        }

        return dropped;
    }

    private static bool Leaves(List<PipelineLinkConfiguration> links, string component, string output) =>
        links.Any(link => string.Equals(link.FromComponent, component, StringComparison.OrdinalIgnoreCase) && string.Equals(link.FromOutput, output, StringComparison.OrdinalIgnoreCase));

    private static bool IsEnd(GraphEnd end, string component, string output) =>
        string.Equals(end.Component, component, StringComparison.OrdinalIgnoreCase) && string.Equals(end.Output, output, StringComparison.OrdinalIgnoreCase);

    private static PipelineLinkConfiguration Link(GraphDataSetLinkConfiguration source) => new()
    {
        Id = source.Id,
        FromComponent = source.FromComponent,
        FromOutput = source.FromOutput,
        ToComponent = source.ToComponent,
        ToInput = source.ToInput,
    };

    private static PipelineComponentConfiguration Component(GraphDataSetComponentConfiguration source) => new()
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
