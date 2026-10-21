using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a union all: the rows of several inputs in one output.</summary>
[TypeOption(typeof(FlowComponentBuilders), "UnionAll")]
public sealed class UnionAllFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="UnionAllFlowComponentBuilder"/> class.</summary>
    public UnionAllFlowComponentBuilder() : base("UnionAll")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        return GenericResult<IFlowComponent>.Success(new FlowUnionAll(component.Name, context.InputsOf(component.Name)));
    }
}
