using System;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds the Compute tool.</summary>
[TypeOption(typeof(FlowComponentBuilders), "Compute")]
public sealed class ComputeFlowComponentBuilder : StageToolFlowComponentBuilder
{
    private const string ReplaceRoute = "Replace";
    private const string StageSource = "Stage";
    private const string DataSetSource = "DataSet";

    /// <summary>Initializes a new instance of the <see cref="ComputeFlowComponentBuilder"/> class.</summary>
    public ComputeFlowComponentBuilder() : base("Compute")
    {
    }

    /// <inheritdoc />
    // Why only these routes and sources: a graph passes rows between tools by links, so an output kept under a name for a later stage
    // to read, or a table read from such an output, has no place here. The tool's rows arrive on its input and leave on its output.
    protected override IGenericResult<IFlowComponent>? Unsupported(PipelineComponentConfiguration component, PipelineTransformConfiguration stage, FlowBuildContext context)
    {
        if (stage.ComputeOutputs.Any(output => !string.Equals(output.Route, ReplaceRoute, StringComparison.Ordinal)))
        {
            return Refuse(component, context, "an output routed to a name for a later stage to read has no place in a graph; route it to Replace and link the tool's output");
        }

        if (stage.ComputeTables.Any(table => !string.Equals(table.FromKind, StageSource, StringComparison.Ordinal)
            && !string.Equals(table.FromKind, DataSetSource, StringComparison.Ordinal)))
        {
            return Refuse(component, context, "a table read from an output kept by an earlier stage has no place in a graph; read the stage's rows or a data set");
        }

        return null;
    }

    private static IGenericResult<IFlowComponent> Refuse(PipelineComponentConfiguration component, FlowBuildContext context, string reason) =>
        GenericResult<IFlowComponent>.Failure(GraphLog.ComponentInvalid(context.Logger, component.Name, component.ComponentType, reason));
}
