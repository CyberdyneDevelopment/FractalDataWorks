using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Database-backed configuration for a pipeline transform step.
/// Child of an ETL pipeline implementation (pipe.BatchCopyPipeline / pipe.StreamingPipeline).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformConfiguration : IGenericConfiguration
{

    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display/binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type (Map, Filter, Calculate, Lookup, Aggregate).
    /// Maps to the pipe.PipelineOperation.OperationType column (FDW-389 rename); resolved at runtime
    /// against the TransformTypes registry.
    /// </summary>
    [ValuesFrom(typeof(TransformTypes))]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution order within the pipeline.
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    /// Gets or sets whether this transform is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the filter expression (for Filter transforms).
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Gets or sets the field mappings (for Map transforms).
    /// </summary>
    public IList<PipelineTransformFieldMappingConfiguration> FieldMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the group-by fields (for Aggregate transforms). For LatestBy and Distinct transforms these are the key
    /// fields: the rows sharing a key value form one group.
    /// </summary>
    public IList<PipelineTransformGroupByFieldConfiguration> GroupByFields { get; set; } = [];

    /// <summary>
    /// Gets or sets the sort fields (for Sort transforms), ordered by their ordinal.
    /// </summary>
    public IList<PipelineTransformSortFieldConfiguration> SortFields { get; set; } = [];

    /// <summary>
    /// Gets or sets the order field (for LatestBy transforms): within a key's group, the row whose value of this field
    /// is chosen by <see cref="KeepRule"/> is the one kept.
    /// </summary>
    public string? OrderField { get; set; }

    /// <summary>
    /// Gets or sets the keep rule (for LatestBy transforms): a <see cref="Fdw.Services.Etl.Abstractions.OptionTypes.KeepRules"/>
    /// option name, First or Last.
    /// </summary>
    [ValuesFrom(typeof(KeepRules))]
    public string? KeepRule { get; set; }

    /// <summary>
    /// Gets or sets the aggregations to apply (for Aggregate transforms).
    /// </summary>
    public IList<PipelineTransformAggregationConfiguration> Aggregations { get; set; } = [];

    /// <summary>
    /// Gets or sets the calculations to apply (for Calculate transforms).
    /// </summary>
    public IList<PipelineTransformCalculationConfiguration> Calculations { get; set; } = [];

    /// <summary>
    /// Gets or sets the lookups to apply (for Lookup transforms) — one row per brought-across column.
    /// </summary>
    public IList<PipelineTransformLookupConfiguration> Lookups { get; set; } = [];

    /// <summary>
    /// Gets or sets what a Validate stage does with a record that fails a check: a <see cref="ValidateFailActions"/> option
    /// name (Reject or Fail). Stated for a Validate stage, never assumed; null on every other kind of stage.
    /// </summary>
    [ValuesFrom(typeof(ValidateFailActions))]
    public string? OnFail { get; set; }

    /// <summary>
    /// Gets or sets the checks of a Validate stage, asked in <see cref="PipelineTransformCheckConfiguration.Ordinal"/> order.
    /// Cascade child (pipe.PipelineTransformCheck).
    /// </summary>
    public IList<PipelineTransformCheckConfiguration> Checks { get; set; } = [];

    /// <summary>
    /// Gets or sets what a Branch stage does with a record that matches none of its paths: an <see cref="UnmatchedActions"/>
    /// option name (Pass or Reject). Stated for a Branch stage, never assumed; null on every other kind of stage.
    /// </summary>
    [ValuesFrom(typeof(UnmatchedActions))]
    public string? Unmatched { get; set; }

    /// <summary>
    /// Gets or sets the paths of a Branch stage, tried in <see cref="PipelineTransformPathConfiguration.Ordinal"/> order; the
    /// first whose condition is true takes the record. Cascade child (pipe.PipelineTransformPath), which in turn owns the stages
    /// of its path.
    /// </summary>
    public IList<PipelineTransformPathConfiguration> Paths { get; set; } = [];

    /// <summary>
    /// Gets or sets the run point a Notify stage fires at: a <see cref="RunPoints"/> option name (AfterStage, AfterLoad or
    /// OnFailure). Stated for a Notify stage, never assumed; null on every other kind of stage.
    /// </summary>
    [ValuesFrom(typeof(RunPoints))]
    public string? RunPoint { get; set; }

    /// <summary>
    /// Gets or sets the name of the notification rule a Notify stage sends through; the rule chooses channel and severity and
    /// its own conditions still decide whether it sends. Stated for a Notify stage; null on every other kind of stage.
    /// </summary>
    public string? NotificationRuleName { get; set; }

    /// <summary>
    /// Gets or sets the message a Notify stage sends, a template that may use the reserved variables of a run in double braces.
    /// Stated for a Notify stage, never assumed; null on every other kind of stage.
    /// </summary>
    public string? NotificationMessage { get; set; }

    /// <summary>
    /// Gets or sets the name of the data set a Union stage appends after the last row of the pipeline's source (FDW-1120 phase 4).
    /// Stated for a Union stage, never assumed; null on every other kind of stage. The stage's <see cref="FieldMappings"/> say how
    /// its fields become the fields the stage outputs.
    /// </summary>
    public string? UnionSourceDataSet { get; set; }

    /// <summary>
    /// Gets or sets the id of the Built Transformation a Compute stage applies to each record (FDW-1209). Stated for a Compute
    /// stage, never assumed; null on every other kind of stage. The run resolves it when it starts and fails, naming the stage,
    /// when it no longer resolves, is not a Built transformation, or does not compile.
    /// </summary>
    public Guid? ComputeTransformationId { get; set; }

    /// <summary>
    /// Gets or sets which field of the record feeds each input the Built Transformation declares; empty on every other kind of stage.
    /// </summary>
    public IList<PipelineTransformComputeInputConfiguration> ComputeInputs { get; set; } = [];

    /// <summary>
    /// Gets or sets where each table input of the Built Transformation gets its rows from (FDW-1231); empty on every other kind of
    /// stage and for a Built Transformation that reads one record at a time.
    /// </summary>
    public IList<PipelineTransformComputeTableConfiguration> ComputeTables { get; set; } = [];

    /// <summary>
    /// Gets or sets where each table output of the Built Transformation goes (FDW-1231); empty when it has none.
    /// </summary>
    public IList<PipelineTransformComputeOutputConfiguration> ComputeOutputs { get; set; } = [];

    /// <summary>
    /// Gets or sets the single values handed to inputs of the Built Transformation (FDW-1231); empty when it needs none.
    /// </summary>
    public IList<PipelineTransformComputeScalarConfiguration> ComputeScalars { get; set; } = [];

    /// <summary>
    /// Gets or sets what a Compute stage that reads and writes tables does with a row it cannot build: Fail, Skip or Reject
    /// (<see cref="Fdw.Services.Etl.Abstractions.RowFailureActions"/>). Null for a stage that states none; a stage that reads and
    /// writes tables must state it, and the run refuses one that does not.
    /// </summary>
    public string? ComputeOnRowFailure { get; set; }
}
