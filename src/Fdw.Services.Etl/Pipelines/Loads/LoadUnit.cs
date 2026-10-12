using System;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// One executable load of a run: the pipeline's own load or one enabled load step, with its kind and where it writes (FDW-1109).
/// </summary>
public sealed class LoadUnit
{
    private LoadUnit(string owner, ILoadStepKind kind, PipelineLoadStepConfiguration row)
    {
        Owner = owner;
        Kind = kind;
        Row = row;
    }

    /// <summary>Gets what the load is, for messages.</summary>
    public string Owner { get; }

    /// <summary>Gets the kind of load.</summary>
    public ILoadStepKind Kind { get; }

    /// <summary>Gets the configuration row: columns, value sources, versioning and soft delete.</summary>
    public PipelineLoadStepConfiguration Row { get; }

    /// <summary>Gets the destination data set, or <see langword="null"/> when the load writes to a named container.</summary>
    public string? DataSet { get; private init; }

    /// <summary>Gets the destination data store when the load writes to a named container.</summary>
    public string? DataStore { get; private init; }

    /// <summary>Gets the path in the destination data store when the load writes to a named container; empty when it states none.</summary>
    public string? Path { get; private init; }

    /// <summary>Gets the destination container when the load writes to a named container.</summary>
    public string? Container { get; private init; }

    /// <summary>Makes the unit of a load step.</summary>
    /// <param name="step">The step row; the validator has already checked it.</param>
    /// <returns>The unit.</returns>
    public static LoadUnit ForStep(PipelineLoadStepConfiguration step)
    {
        var kind = LoadStepKinds.ByName(step.Kind!);
        var owner = string.Concat("load step '", step.Name, "'");
        return kind.UsesDestinationContainer
            ? new LoadUnit(owner, kind, step)
            {
                DataStore = step.DestinationDataStoreName,
                Path = step.DestinationPathName,
                Container = step.DestinationContainerName,
            }
            : new LoadUnit(owner, kind, step) { DataSet = step.DestinationDataSet };
    }

    /// <summary>Makes the unit of the pipeline's own load, which writes where the pipeline's destination says.</summary>
    /// <param name="configuration">The pipeline.</param>
    /// <param name="row">The row of the pipeline's own load (its options and value sources).</param>
    /// <param name="kind">The kind of load the load mode means.</param>
    /// <returns>The unit.</returns>
    public static LoadUnit ForOwnLoad(BatchCopyPipelineConfiguration configuration, PipelineLoadStepConfiguration row, ILoadStepKind kind)
    {
        const string owner = "the pipeline's own load";
        return string.IsNullOrWhiteSpace(configuration.DestinationDataSet)
            ? new LoadUnit(owner, kind, row)
            {
                DataStore = configuration.DestinationConnectionName,
                Path = string.Empty,
                Container = configuration.DestinationContainerPath,
            }
            : new LoadUnit(owner, kind, row) { DataSet = configuration.DestinationDataSet };
    }
}
