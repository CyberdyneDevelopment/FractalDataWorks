using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a step that reads the data configuration rules of a column and keeps the list of values they pick in a variable (FDW-5347, FDW-6344).</summary>
[TypeOption(typeof(FlowStepBuilders), "ReadDataOptions")]
public sealed class ReadDataOptionsFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ReadDataOptionsFlowStepBuilder"/> class.</summary>
    public ReadDataOptionsFlowStepBuilder() : base("ReadDataOptions")
    {
    }

    /// <inheritdoc />
    public override bool HasOutsideEffect => true;

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.OptionsDataStoreName is not { } optionsDataStore
            || component.OptionsContainerName is not { } rulesContainer
            || component.OptionValuesContainerName is not { } valuesContainer
            || component.DataStoreName is not { } dataStore
            || component.PathName is not { } pathName
            || component.ContainerName is not { } container
            || component.FieldName is not { } column
            || component.MaxRows is not { } maxRows
            || component.ResultVariable is not { } resultVariable)
        {
            return Missing(component, context);
        }

        if (context.EtlGateway is not { } gateway)
        {
            return GenericResult<IFlowStep>.Failure(GraphLog.RunDependencyMissing(context.Logger, context.PipelineName, "the ETL gateway"));
        }

        return GenericResult<IFlowStep>.Success(new FlowReadDataOptions(
            component.Name, gateway, optionsDataStore, rulesContainer, valuesContainer, dataStore, pathName, container, column, maxRows, resultVariable, context.Logger));
    }
}
