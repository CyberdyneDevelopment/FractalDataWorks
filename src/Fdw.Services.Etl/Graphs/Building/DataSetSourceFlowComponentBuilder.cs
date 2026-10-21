using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a source that reads a data set through the data gateway.</summary>
[TypeOption(typeof(FlowComponentBuilders), "DataSetSource")]
public sealed class DataSetSourceFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetSourceFlowComponentBuilder"/> class.</summary>
    public DataSetSourceFlowComponentBuilder() : base("DataSetSource")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.DataSetName is not { } dataSet || component.BatchSize is not { } batchSize)
        {
            return MissingSettings.Of(component, context);
        }

        return GenericResult<IFlowComponent>.Success(
            FlowGatewaySource.ForDataSet(component.Name, context.Gateway, dataSet, batchSize, context.Logger, context.Test?.MaxRowsPerSource));
    }
}
