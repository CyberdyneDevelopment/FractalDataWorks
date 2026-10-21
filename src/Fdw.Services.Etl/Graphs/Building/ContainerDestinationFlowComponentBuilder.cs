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

/// <summary>Builds a destination that loads one container of a data store.</summary>
[TypeOption(typeof(FlowComponentBuilders), "ContainerDestination")]
public sealed class ContainerDestinationFlowComponentBuilder : DestinationFlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerDestinationFlowComponentBuilder"/> class.</summary>
    public ContainerDestinationFlowComponentBuilder() : base("ContainerDestination")
    {
    }

    /// <inheritdoc />
    protected override LoadUnit? UnitFor(PipelineComponentConfiguration component, ILoadStepKind kind, PipelineLoadStepConfiguration row) =>
        component.DataStoreName is { } dataStore && component.ContainerName is { } container
            ? LoadUnit.ForContainer(
                string.Concat("graph destination '", component.Name, "'"), kind, row, dataStore, component.PathName is { } path ? path : string.Empty, container)
            : null;
}
