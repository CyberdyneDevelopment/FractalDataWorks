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

/// <summary>Builds a merge join: two inputs joined on key fields.</summary>
[TypeOption(typeof(FlowComponentBuilders), "MergeJoin")]
public sealed class MergeJoinFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="MergeJoinFlowComponentBuilder"/> class.</summary>
    public MergeJoinFlowComponentBuilder() : base("MergeJoin")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.JoinKind is not { } joinKindName || component.RightPrefix is not { } rightPrefix || component.BatchSize is not { } batchSize)
        {
            return MissingSettings.Of(component, context);
        }

        var kind = JoinKinds.ByName(joinKindName);
        if (ReferenceEquals(kind, JoinKinds.NotFound))
        {
            return GenericResult<IFlowComponent>.Failure(
                GraphLog.ChoiceUnknown(context.Logger, component.Name, "joinKind", joinKindName, nameof(JoinKinds)));
        }

        return GenericResult<IFlowComponent>.Success(new FlowMergeJoin(
            component.Name, Columns(component, "leftKeys"), Columns(component, "rightKeys"), kind, rightPrefix, batchSize, context.Logger));
    }
}
