using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;

namespace Fdw.Services.Etl.Tests.Graphs.Contributions;

/// <summary>A step builder from another package (FDW-6240): it names the types it needs, and nothing else; built directly, not registered.</summary>
internal sealed class NeedingStepBuilder : FlowStepBuilderBase
{
    private readonly IReadOnlyList<Type> _needs;

    public NeedingStepBuilder(string name, params Type[] needs) : base(name)
    {
        _needs = needs;
    }

    public override IReadOnlyList<Type> Needs => _needs;

    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context) =>
        GenericResult<IFlowStep>.Success(new FlowSkippedStep(component.Name, component.ComponentType, context.Logger));
}
