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

    /// <summary>Gets where the load writes when it states a file or files and not the container as declared (FDW-3193); <see langword="null"/> otherwise.</summary>
    public LoadAddressing? Addressing { get; private init; }

    /// <summary>
    /// Gets the key this load stages its keys under (FDW-4956): an absent-key soft delete appends each batch's keys to its stage container with this
    /// value and sweeps once at the end; <see langword="null"/> for a load that stages nothing.
    /// </summary>
    public string? StageKey { get; private init; }

    /// <summary>
    /// Gets how many unmatched keys this load's report carries (FDW-4988), for a load that finds existing rows by key; <see langword="null"/> for one that
    /// matches nothing by key.
    /// </summary>
    public int? UnmatchedKeySample { get; private init; }

    /// <summary>Makes the unit of a load step.</summary>
    /// <param name="step">The step row; the validator has already checked it.</param>
    /// <returns>The unit.</returns>
    public static LoadUnit ForStep(PipelineLoadStepConfiguration step)
    {
        var kind = LoadStepKinds.ByName(step.Kind!);
        var owner = string.Concat("load step '", step.Name, "'");
        return new LoadUnit(owner, kind, step) { DataSet = step.DestinationDataSet };
    }

    /// <summary>Makes the unit of a graph destination that loads a data set.</summary>
    /// <param name="owner">What the load is, for messages.</param>
    /// <param name="kind">The kind of load.</param>
    /// <param name="row">The row that carries the load's options and columns.</param>
    /// <param name="dataSet">The destination data set.</param>
    /// <returns>The unit.</returns>
    public static LoadUnit ForDataSet(string owner, ILoadStepKind kind, PipelineLoadStepConfiguration row, string dataSet)
        => new(owner, kind, row) { DataSet = dataSet };

    /// <summary>Makes the unit of a graph destination that loads a named container of a data store.</summary>
    /// <param name="owner">What the load is, for messages.</param>
    /// <param name="kind">The kind of load.</param>
    /// <param name="row">The row that carries the load's options and columns.</param>
    /// <param name="dataStore">The destination data store.</param>
    /// <param name="path">The path in the data store; empty when it states none.</param>
    /// <param name="container">The destination container.</param>
    /// <returns>The unit.</returns>
    public static LoadUnit ForContainer(string owner, ILoadStepKind kind, PipelineLoadStepConfiguration row, string dataStore, string path, string container)
        => new(owner, kind, row) { DataStore = dataStore, Path = path, Container = container };

    /// <summary>Makes the same load addressed at a file or files (FDW-3193).</summary>
    /// <param name="addressing">Where it writes; <see langword="null"/> leaves the load as it is.</param>
    /// <returns>The load with the addressing.</returns>
    public LoadUnit WithAddressing(LoadAddressing? addressing)
        => addressing is null ? this : new LoadUnit(Owner, Kind, Row) { DataSet = DataSet, DataStore = DataStore, Path = Path, Container = Container, Addressing = addressing, StageKey = StageKey, UnmatchedKeySample = UnmatchedKeySample };

    /// <summary>Makes the same load staging its keys under <paramref name="stageKey"/> (FDW-4956).</summary>
    /// <param name="stageKey">The value that tells this load's stage rows apart: the run's execution id and the component's name.</param>
    /// <returns>The load with the stage key.</returns>
    public LoadUnit WithStageKey(string stageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stageKey);
        return new LoadUnit(Owner, Kind, Row) { DataSet = DataSet, DataStore = DataStore, Path = Path, Container = Container, Addressing = Addressing, StageKey = stageKey, UnmatchedKeySample = UnmatchedKeySample };
    }

    /// <summary>Makes the same load reporting up to <paramref name="unmatchedKeySample"/> keys that matched no row (FDW-4988).</summary>
    /// <param name="unmatchedKeySample">How many unmatched keys a report carries; zero carries only the count.</param>
    /// <returns>The load with the sample size.</returns>
    public LoadUnit WithUnmatchedKeySample(int unmatchedKeySample)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unmatchedKeySample);
        return new LoadUnit(Owner, Kind, Row) { DataSet = DataSet, DataStore = DataStore, Path = Path, Container = Container, Addressing = Addressing, StageKey = StageKey, UnmatchedKeySample = unmatchedKeySample };
    }
}
