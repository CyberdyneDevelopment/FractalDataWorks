using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;

namespace Fdw.Services.Etl.Tests.Graphs.Builders;

/// <summary>A component builder from another package (FDW-6240), built directly and not registered; its name is chosen by each test.</summary>
internal sealed class NamedComponentBuilder : FlowComponentBuilderBase
{
    public NamedComponentBuilder(string name) : base(name)
    {
    }

    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context) =>
        GenericResult<IFlowComponent>.Success(new FlowDestination(component.Name, (_, _) => Task.FromResult(GenericResult.Success())));
}
