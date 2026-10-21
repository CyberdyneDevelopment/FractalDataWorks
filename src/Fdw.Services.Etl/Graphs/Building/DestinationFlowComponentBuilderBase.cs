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

/// <summary>
/// Base for the builders of the two destination kinds: the load, and the truncate that precedes the first batch, are the same
/// units a pipeline's own load runs through; only where they write differs.
/// </summary>
public abstract class DestinationFlowComponentBuilderBase : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="DestinationFlowComponentBuilderBase"/> class.</summary>
    /// <param name="name">The component kind this builder builds.</param>
    protected DestinationFlowComponentBuilderBase(string name) : base(name)
    {
    }

    /// <summary>Makes the unit that loads this component's target with a kind of load.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="kind">The kind of load.</param>
    /// <param name="row">The row that carries the load's options and columns.</param>
    /// <returns>The unit, or <see langword="null"/> when the component does not state where it writes.</returns>
    protected abstract LoadUnit? UnitFor(PipelineComponentConfiguration component, ILoadStepKind kind, PipelineLoadStepConfiguration row);

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.LoadKind is not { } loadKindName || component.TruncateFirst is not { } truncateFirst)
        {
            return MissingSettings.Of(component, context);
        }

        var kind = LoadStepKinds.ByName(loadKindName);
        if (ReferenceEquals(kind, LoadStepKinds.NotFound))
        {
            return GenericResult<IFlowComponent>.Failure(
                GraphLog.ChoiceUnknown(context.Logger, component.Name, "loadKind", loadKindName, nameof(LoadStepKinds)));
        }

        if (!kind.UsesRows || kind.UsesSourceDataSet || kind.UsesDestinationContainer)
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.ComponentInvalid(
                context.Logger, component.Name, component.ComponentType,
                $"the load kind '{loadKindName}' does not load the rows a component receives"));
        }

        if (DestinationOptions.ColumnListsBound(component))
        {
            var lists = DestinationOptions.ColumnLists(component, kind, context.Logger);
            if (lists.IsFailure)
            {
                return lists.ToNewResult<IFlowComponent>();
            }
        }

        var unit = UnitFor(component, kind, DestinationOptions.Row(component, kind));
        if (unit is null)
        {
            return MissingSettings.Of(component, context);
        }

        LoadUnit? truncate = null;
        if (truncateFirst)
        {
            var truncateKind = LoadStepKinds.ByName("Truncate");
            truncate = UnitFor(component, truncateKind, DestinationOptions.ColumnsRow(component, truncateKind));
        }

        var destination = new FlowLoadDestination(
            component.Name, context.LoadExecutor, context.PipelineName, unit, truncate, context.Parameters, context.StartedAt, context.Logger, context.Test?.SkipWrites == true);
        context.Add(destination);
        return GenericResult<IFlowComponent>.Success(destination);
    }
}
