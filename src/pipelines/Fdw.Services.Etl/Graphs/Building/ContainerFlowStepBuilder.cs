using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a container (FDW-6134): runs the components inside it once.</summary>
[TypeOption(typeof(FlowStepBuilders), "Container")]
public sealed class ContainerFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerFlowStepBuilder"/> class.</summary>
    public ContainerFlowStepBuilder() : base("Container")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context) =>
        GenericResult<IFlowStep>.Success(new FlowContainer(component.Name, body));
}
