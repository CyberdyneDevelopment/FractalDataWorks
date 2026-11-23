using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;

namespace Fdw.Services.Etl.TestDouble;

/// <summary>The builder of <see cref="NeededSourceComponentType"/> (FDW-6240): it needs <see cref="LookupContribution"/> and reads the data set as the data set source does.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FlowComponentBuilders), "NeededSource")]
public sealed class NeededSourceFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="NeededSourceFlowComponentBuilder"/> class.</summary>
    public NeededSourceFlowComponentBuilder() : base("NeededSource")
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<Type> Needs => [typeof(LookupContribution)];

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        var lookup = context.Contribution<LookupContribution>(component);
        if (lookup.IsFailure || lookup.Value is null)
        {
            return lookup.ToNewResult<IFlowComponent>();
        }

        lookup.Value.Used = true;
        return new DataSetSourceFlowComponentBuilder().Create(component, context);
    }
}
