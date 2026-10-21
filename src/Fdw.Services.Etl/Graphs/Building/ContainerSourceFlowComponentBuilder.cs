using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a source that reads one container of a data store through the data gateway.</summary>
[TypeOption(typeof(FlowComponentBuilders), "ContainerSource")]
public sealed class ContainerSourceFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerSourceFlowComponentBuilder"/> class.</summary>
    public ContainerSourceFlowComponentBuilder() : base("ContainerSource")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.DataStoreName is not { } dataStore || component.ContainerName is not { } container || component.BatchSize is not { } batchSize)
        {
            return MissingSettings.Of(component, context);
        }

        var overridden = !string.IsNullOrWhiteSpace(component.PhysicalPath) || !string.IsNullOrWhiteSpace(component.PhysicalName);
        if (overridden && context.Gateway is not IEtlDataGateway)
        {
            return GenericResult<IFlowComponent>.Failure(
                GraphLog.ComponentInvalid(context.Logger, component.Name, component.ComponentType, "it states a physical path or name, which only the ETL server's data gateway can read"));
        }

        return GenericResult<IFlowComponent>.Success(
            FlowGatewaySource.ForContainer(
                component.Name, context.Gateway, dataStore, component.PathName, container, batchSize, context.Logger, context.Test?.MaxRowsPerSource,
                component.PhysicalPath, component.PhysicalName));
    }
}
