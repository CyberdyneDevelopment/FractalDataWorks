using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a source that reads the rows a SQL script selects (FDW-1443).</summary>
[TypeOption(typeof(FlowComponentBuilders), "SqlSource")]
public sealed class SqlSourceFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlSourceFlowComponentBuilder"/> class.</summary>
    public SqlSourceFlowComponentBuilder() : base("SqlSource")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.DataStoreName is not { } dataStore || component.ScriptText is not { } script
            || component.MaxRows is not { } maxRows || component.BatchSize is not { } batchSize)
        {
            return MissingSettings.Of(component, context);
        }

        if (context.EtlGateway is not { } gateway)
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.RunDependencyMissing(context.Logger, context.PipelineName, "the ETL gateway"));
        }

        var parameters = ScriptParameters.Of(component, ScriptParameters.Setting, context);
        if (parameters.IsFailure || parameters.Value is null)
        {
            return parameters.ToNewResult<IFlowComponent>();
        }

        return GenericResult<IFlowComponent>.Success(
            new FlowScriptSource(component.Name, gateway, dataStore, script, parameters.Value, maxRows, batchSize, context.Logger, context.Test?.MaxRowsPerSource));
    }
}
