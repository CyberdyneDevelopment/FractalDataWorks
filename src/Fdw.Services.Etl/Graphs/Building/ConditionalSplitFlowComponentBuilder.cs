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

/// <summary>Builds a conditional split: each row goes down the output whose filter it meets.</summary>
[TypeOption(typeof(FlowComponentBuilders), "ConditionalSplit")]
public sealed class ConditionalSplitFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionalSplitFlowComponentBuilder"/> class.</summary>
    public ConditionalSplitFlowComponentBuilder() : base("ConditionalSplit")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.Routing is not { } routingName)
        {
            return MissingSettings.Of(component, context);
        }

        var routing = SplitRoutings.ByName(routingName);
        if (ReferenceEquals(routing, SplitRoutings.NotFound))
        {
            return GenericResult<IFlowComponent>.Failure(
                GraphLog.ChoiceUnknown(context.Logger, component.Name, "routing", routingName, nameof(SplitRoutings)));
        }

        var outputs = new List<FlowSplitOutput>();
        foreach (var output in component.SplitOutputs.OrderBy(o => o.Ordinal))
        {
            var filter = FlowPredicate.FromExpression(output.FilterExpression, context.Logger);
            if (filter.IsFailure || filter.Value is null)
            {
                return filter.ToNewResult<IFlowComponent>();
            }

            outputs.Add(new FlowSplitOutput(output.OutputName, filter.Value));
        }

        return GenericResult<IFlowComponent>.Success(
            new FlowConditionalSplit(component.Name, outputs, routing, Unmatched(component), context.Logger));
    }
}
