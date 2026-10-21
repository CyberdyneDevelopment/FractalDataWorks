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

/// <summary>Builds a lookup: columns of a reference input added to each row with a matching key.</summary>
[TypeOption(typeof(FlowComponentBuilders), "Lookup")]
public sealed class LookupFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="LookupFlowComponentBuilder"/> class.</summary>
    public LookupFlowComponentBuilder() : base("Lookup")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.BatchSize is not { } batchSize)
        {
            return MissingSettings.Of(component, context);
        }

        var bring = Maps(component, "bring", null, context);
        if (bring.IsFailure || bring.Value is null)
        {
            return bring.ToNewResult<IFlowComponent>();
        }

        return GenericResult<IFlowComponent>.Success(new FlowLookup(
            component.Name, Columns(component, "keys"), Columns(component, "referenceKeys"), bring.Value, Unmatched(component), batchSize, context.Logger));
    }
}
