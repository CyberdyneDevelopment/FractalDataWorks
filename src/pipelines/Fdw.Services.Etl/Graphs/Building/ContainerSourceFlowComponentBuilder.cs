using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Builds a source that reads one container of a data store through the data gateway, or, for the Files target kind, a list of files of that
/// container as one stream (FDW-3089).
/// </summary>
[TypeOption(typeof(FlowComponentBuilders), "ContainerSource")]
public sealed class ContainerSourceFlowComponentBuilder : FlowComponentBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerSourceFlowComponentBuilder"/> class.</summary>
    public ContainerSourceFlowComponentBuilder() : base("ContainerSource")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.DataStoreName is not { } dataStore || component.ContainerName is not { } container || component.BatchSize is not { } batchSize)
        {
            return MissingSettings.Of(component, context);
        }

        var kindProblems = TargetKindRules.Problems(component);
        if (kindProblems.Count > 0)
        {
            return Invalid(component, context, string.Join("; ", kindProblems.Select(problem => problem.Message)));
        }

        var files = component.TargetKind is { } kind && ReferenceEquals(TargetKinds.ByName(kind), TargetKinds.Files);
        var overridden = files || !string.IsNullOrWhiteSpace(component.PhysicalPath) || !string.IsNullOrWhiteSpace(component.PhysicalName);
        if (overridden && context.Gateway is not IEtlDataGateway)
        {
            return Invalid(component, context, "it states a physical path or name, or a list of files, which only the ETL server's data gateway can read");
        }

        return files
            ? CreateFiles(component, dataStore, container, batchSize, context)
            : GenericResult<IFlowComponent>.Success(
                FlowGatewaySource.ForContainer(
                    component.Name, context.Gateway, dataStore, component.PathName, container, batchSize, context.Logger, context.Test?.MaxRowsPerSource,
                    component.PhysicalPath, component.PhysicalName));
    }

    private static IGenericResult<IFlowComponent> CreateFiles(
        PipelineComponentConfiguration component, string dataStore, string container, int batchSize, FlowBuildContext context)
    {
        var names = FileNames(component, context);
        return names.IsFailure || names.Value is null
            ? names.ToNewResult<IFlowComponent>()
            : GenericResult<IFlowComponent>.Success(
                FlowGatewaySource.ForContainerFiles(
                    component.Name, context.Gateway, dataStore, component.PathName, container, batchSize, names.Value, component.PhysicalPath, context.Logger,
                    context.Test?.MaxRowsPerSource));
    }

    // The names come from a list run parameter the run was started with, or from a list variable in scope when the stage starts (the batch of a round of
    // a for each, FDW-3194). A list that is missing, holds text or a single value, is empty, or has an item that is no name fails the run; nothing is
    // treated as a list of one and no file is skipped.
    private static IGenericResult<IReadOnlyList<string>> FileNames(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.FilesList is not { } list)
        {
            return Failed(component, context, "it states no filesList");
        }

        if (!context.Parameters.TryGetValue(list, out var held))
        {
            return Failed(component, context, $"its filesList '{list}' is not a run parameter this run was given nor a variable in scope");
        }

        if (held is string || held is not IEnumerable items)
        {
            return Failed(component, context, $"its filesList '{list}' is not a list");
        }

        var names = new List<string>();
        foreach (var item in items)
        {
            var text = item?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Failed(component, context, $"its filesList '{list}' has an item that is not a file name");
            }

            names.Add(text);
        }

        return names.Count == 0
            ? Failed(component, context, $"its filesList '{list}' is empty, so there is no file to read")
            : GenericResult<IReadOnlyList<string>>.Success(names);
    }

    private static IGenericResult<IReadOnlyList<string>> Failed(PipelineComponentConfiguration component, FlowBuildContext context, string reason) =>
        GenericResult<IReadOnlyList<string>>.Failure(GraphLog.ComponentInvalid(context.Logger, component.Name, component.ComponentType, reason));

    private static IGenericResult<IFlowComponent> Invalid(PipelineComponentConfiguration component, FlowBuildContext context, string reason) =>
        GenericResult<IFlowComponent>.Failure(GraphLog.ComponentInvalid(context.Logger, component.Name, component.ComponentType, reason));
}
