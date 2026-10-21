using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a step that runs a SQL command on a data store (FDW-1443).</summary>
[TypeOption(typeof(FlowStepBuilders), "ExecuteSql")]
public sealed class ExecuteSqlFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteSqlFlowStepBuilder"/> class.</summary>
    public ExecuteSqlFlowStepBuilder() : base("ExecuteSql")
    {
    }

    /// <inheritdoc />
    public override bool HasOutsideEffect => true;

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.DataStoreName is not { } dataStore || component.ScriptText is not { } script)
        {
            return Missing(component, context);
        }

        if (context.EtlGateway is not { } gateway)
        {
            return GenericResult<IFlowStep>.Failure(GraphLog.RunDependencyMissing(context.Logger, context.PipelineName, "the ETL gateway"));
        }

        var parameters = component.Parameters.Select(parameter => new FlowSqlParameter(parameter.ParameterName, VariableText.Typed(parameter.ValueText))).ToList();
        return GenericResult<IFlowStep>.Success(
            new FlowExecuteSql(component.Name, gateway, dataStore, script, parameters, component.ResultVariable, context.Logger));
    }
}
