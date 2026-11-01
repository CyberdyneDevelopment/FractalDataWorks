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

/// <summary>The failure of a builder that finds a setting unstated after the settings were checked.</summary>
internal static class MissingSettings
{
    /// <summary>Makes the failure.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>A failure naming the component and its kind.</returns>
    public static IGenericResult<IFlowComponent> Of(PipelineComponentConfiguration component, FlowBuildContext context) =>
        GenericResult<IFlowComponent>.Failure(
            GraphLog.ComponentInvalid(context.Logger, component.Name, component.ComponentType, "a setting its kind requires is not stated"));
}
