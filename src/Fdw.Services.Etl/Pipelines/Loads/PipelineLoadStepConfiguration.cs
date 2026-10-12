using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// One load step of a BatchCopy pipeline, or the pipeline's own load (FDW-1108). Persisted in
/// <c>pipe.PipelineLoadStep</c>; cascade child of <c>BatchCopyPipelineConfiguration</c> (key
/// <c>BatchCopyPipelineConfigurationRowId</c>).
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="StepOrder"/> of <see langword="null"/> is the pipeline's own load: its destination is the pipeline's
/// destination and its kind is the pipeline's <c>LoadMode</c>, so every step-only field is <see langword="null"/>
/// and the row carries only the load options and the value sources. It exists only when the pipeline has no load
/// steps. A step order of 1 or more is a real step, which names its <see cref="Kind"/>, its
/// <see cref="DestinationDataSet"/> and its <see cref="Name"/>. A step's <see cref="Phase"/> says whether it loads the output
/// rows or, as a reject step, the rejected records; load steps and reject steps are each ordered from 1.
/// </para>
/// <para>
/// Every option is <see langword="null"/> until someone states it; nothing here has a default, and the engine
/// refuses a command whose required option is <see langword="null"/>. An Execute step names the executable container
/// of its destination data store it runs (<see cref="DestinationDataStoreName"/>, <see cref="DestinationPathName"/> and <see cref="DestinationContainerName"/>);
/// what that means is the destination connection type's alone.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineLoadStepConfiguration : IGenericConfiguration
{
    /// <summary>
    /// The name the row of the pipeline's own load is stored under.
    /// </summary>
    public const string PipelineLoadName = "PipelineLoad";

    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the step name, unique within the pipeline; <see cref="PipelineLoadName"/> on the pipeline's own load.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the 1-based position of the step, or <see langword="null"/> on the pipeline's own load.
    /// </summary>
    public int? StepOrder { get; set; }

    /// <summary>
    /// Gets or sets when in a run the row is written and what it writes, resolved against <see cref="LoadPhases"/>: Load for the
    /// pipeline's own load and its load steps, Reject for a reject step, which writes the records the pipeline's stages rejected
    /// (FDW-1142). Always stated: nothing defaults it, and a row that names none is refused.
    /// </summary>
    [ValuesFrom(typeof(LoadPhases))]
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets what the step does, resolved against <see cref="LoadStepKinds"/>. Required on a step; <see langword="null"/> on the pipeline's own load.
    /// </summary>
    [ValuesFrom(typeof(LoadStepKinds))]
    public string? Kind { get; set; }

    /// <summary>
    /// Gets or sets the data set the step writes to. Required on a step; <see langword="null"/> on the pipeline's own load.
    /// </summary>
    public string? DestinationDataSet { get; set; }

    /// <summary>
    /// Gets or sets the data set an InsertFromQuery step reads from (same connection as the destination), or <see langword="null"/>.
    /// </summary>
    public string? SourceDataSet { get; set; }

    /// <summary>
    /// Gets or sets the filter expression of an InsertFromQuery step, or <see langword="null"/>.
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Gets or sets the data store, one of the destination data set's sources' stores, whose executable container an Execute step runs. Stated together with <see cref="DestinationPathName"/> and <see cref="DestinationContainerName"/>.
    /// </summary>
    public string? DestinationDataStoreName { get; set; }

    /// <summary>
    /// Gets or sets the path of the executable container an Execute step runs, in the data store. Stated together with <see cref="DestinationDataStoreName"/> and <see cref="DestinationContainerName"/>.
    /// </summary>
    public string? DestinationPathName { get; set; }

    /// <summary>
    /// Gets or sets the executable container an Execute step runs, in the data store. Stated together with <see cref="DestinationDataStoreName"/> and <see cref="DestinationPathName"/>.
    /// </summary>
    public string? DestinationContainerName { get; set; }

    /// <summary>
    /// Gets or sets the column that holds when a row version starts to be current (versioned upsert). Stated together with <see cref="EffectiveToColumn"/>.
    /// </summary>
    public string? EffectiveFromColumn { get; set; }

    /// <summary>
    /// Gets or sets the column that holds when a row version stops being current (versioned upsert). Stated together with <see cref="EffectiveFromColumn"/>.
    /// </summary>
    public string? EffectiveToColumn { get; set; }

    /// <summary>
    /// Gets or sets the optional column that flags the current version of a row (versioned upsert).
    /// </summary>
    public string? CurrentFlagColumn { get; set; }

    /// <summary>
    /// Gets or sets the column whose value marks a row as insert-only: such a row is never modified. Stated together with <see cref="InsertOnlyRuleValue"/>, and only with versioning.
    /// </summary>
    public string? InsertOnlyRuleColumn { get; set; }

    /// <summary>
    /// Gets or sets the value that marks a row as insert-only. Stated together with <see cref="InsertOnlyRuleColumn"/>.
    /// </summary>
    public string? InsertOnlyRuleValue { get; set; }

    /// <summary>
    /// Gets or sets what makes a row soft deleted, resolved against <see cref="SoftDeleteModes"/>, or <see langword="null"/> for a hard delete.
    /// </summary>
    [ValuesFrom(typeof(SoftDeleteModes))]
    public string? SoftDeleteMode { get; set; }

    /// <summary>
    /// Gets or sets the deleted-flag column a soft delete sets instead of removing the row.
    /// </summary>
    public string? DeletedFlagColumn { get; set; }

    /// <summary>
    /// Gets or sets the optional column a soft delete stamps with the time of the delete.
    /// </summary>
    public string? DeletedAtColumn { get; set; }

    /// <summary>
    /// Gets or sets the marker column of the <c>MarkerMatches</c> mode.
    /// </summary>
    public string? SoftDeleteMarkerColumn { get; set; }

    /// <summary>
    /// Gets or sets the marker value of the <c>MarkerMatches</c> mode.
    /// </summary>
    public string? SoftDeleteMarkerValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the step is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the key, compare and write column lists, each ordered by role and ordinal. Rows live in <c>pipe.PipelineLoadColumn</c>, each holding this row's key in <c>PipelineLoadStepRowId</c>.
    /// </summary>
    public IList<PipelineLoadColumnConfiguration> Columns { get; set; } = [];

    /// <summary>
    /// Gets or sets the value sources of the step (the parameters, on an Execute step). Rows live in <c>pipe.PipelineLoadValueSource</c>.
    /// </summary>
    public IList<PipelineLoadValueSourceConfiguration> ValueSources { get; set; } = [];
}
