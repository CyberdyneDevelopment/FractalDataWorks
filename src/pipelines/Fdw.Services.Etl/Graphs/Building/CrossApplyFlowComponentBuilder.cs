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

/// <summary>Builds a cross apply: a list-valued or delimited field turned into one row per value.</summary>
[TypeOption(typeof(FlowComponentBuilders), "CrossApply")]
public sealed class CrossApplyFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="CrossApplyFlowComponentBuilder"/> class.</summary>
    public CrossApplyFlowComponentBuilder() : base("CrossApply")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.FieldName is not { } field || component.TargetName is not { } target || component.KeepsEmpty is not { } keepsEmpty)
        {
            return MissingSettings.Of(component, context);
        }

        return GenericResult<IFlowComponent>.Success(
            new FlowCrossApply(component.Name, field, target, component.Delimiter, keepsEmpty, context.Logger));
    }
}
