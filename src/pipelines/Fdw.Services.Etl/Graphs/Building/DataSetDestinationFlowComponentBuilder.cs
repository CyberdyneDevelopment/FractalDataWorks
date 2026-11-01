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

/// <summary>Builds a destination that loads a data set.</summary>
[TypeOption(typeof(FlowComponentBuilders), "DataSetDestination")]
public sealed class DataSetDestinationFlowComponentBuilder : DestinationFlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetDestinationFlowComponentBuilder"/> class.</summary>
    public DataSetDestinationFlowComponentBuilder() : base("DataSetDestination")
    {
    }

    /// <inheritdoc />
    protected override LoadUnit? UnitFor(PipelineComponentConfiguration component, ILoadStepKind kind, PipelineLoadStepConfiguration row) =>
        component.DataSetName is { } dataSet
            ? LoadUnit.ForDataSet(string.Concat("graph destination '", component.Name, "'"), kind, row, dataSet)
            : null;
}
