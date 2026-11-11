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

        if (!kind.UsesRows || kind.UsesSourceDataSet)
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.ComponentInvalid(
                context.Logger, component.Name, component.ComponentType,
                $"the load kind '{loadKindName}' does not load the rows a component receives"));
        }

        var kindProblems = TargetKindRules.WriteProblems(component);
        if (kindProblems.Count > 0)
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.ComponentInvalid(
                context.Logger, component.Name, component.ComponentType, string.Join("; ", kindProblems.Select(problem => problem.Message))));
        }

        var addressing = LoadAddressing.Of(component.TargetKind, component.PhysicalPath, component.PhysicalName, component.SplitField);
        if (DestinationOptions.ColumnListsBound(component))
        {
            var lists = DestinationOptions.ColumnLists(component, kind, context.Logger);
            if (lists.IsFailure)
            {
                return lists.ToNewResult<IFlowComponent>();
            }
        }

        var unit = UnitOf(component, kind, context, addressing);
        if (unit.IsFailure || unit.Value is null)
        {
            return unit.ToNewResult<IFlowComponent>();
        }

        var destination = new FlowLoadDestination(
            component.Name, context.LoadExecutor, context.PipelineName, unit.Value, TruncateFor(component, truncateFirst, addressing), context.Parameters, context.StartedAt,
            context.CommitReportInterval, context.Logger, context.Test?.SkipWrites == true);
        context.Add(destination);
        return GenericResult<IFlowComponent>.Success(destination);
    }

    // The load each batch goes through: the unit of the component's target, at its file when it states one, staging its keys when it must (FDW-4956), and
    // reporting as many unmatched keys as the run's sample size when it finds rows by key (FDW-4988).
    private IGenericResult<LoadUnit> UnitOf(PipelineComponentConfiguration component, ILoadStepKind kind, FlowBuildContext context, LoadAddressing? addressing)
    {
        var unit = UnitFor(component, kind, DestinationOptions.Row(component, kind))?.WithAddressing(addressing);
        if (unit is null)
        {
            return MissingSettings.Of(component, context).ToNewResult<LoadUnit>();
        }

        var reporting = PipelineLoadValidator.MatchesByKey(unit.Row, kind) ? unit.WithUnmatchedKeySample(context.UnmatchedKeySample) : unit;
        return Staged(component, context, reporting, addressing);
    }

    // The truncate that empties the target once before the first batch, when the component replaces it; null when it does not.
    private LoadUnit? TruncateFor(PipelineComponentConfiguration component, bool truncateFirst, LoadAddressing? addressing)
    {
        if (!truncateFirst)
        {
            return null;
        }

        var truncateKind = LoadStepKinds.ByName("Truncate");
        return UnitFor(component, truncateKind, DestinationOptions.ColumnsRow(component, truncateKind))?.WithAddressing(addressing);
    }

    // An absent-key soft delete into a database stages its keys under the run's execution id and the component's name (FDW-4956): component names are
    // unique in a graph, a loop's rounds run one after another and each clears its stage first, and every other run has its own execution id. A file
    // destination holds its rows instead, as before, and any other load is returned as it is.
    private static IGenericResult<LoadUnit> Staged(PipelineComponentConfiguration component, FlowBuildContext context, LoadUnit unit, LoadAddressing? addressing)
    {
        if (addressing is not null || !DestinationOptions.StagesKeys(component))
        {
            return GenericResult<LoadUnit>.Success(unit);
        }

        return context.ExecutionId == Guid.Empty
            ? GenericResult<LoadUnit>.Failure(GraphLog.ComponentInvalid(
                context.Logger, component.Name, component.ComponentType, "an absent-from-source soft delete stages its keys under the run's execution id, and this run has none"))
            : GenericResult<LoadUnit>.Success(unit.WithStageKey(string.Concat(context.ExecutionId.ToString("N", System.Globalization.CultureInfo.InvariantCulture), "/", component.Name)));
    }
}
