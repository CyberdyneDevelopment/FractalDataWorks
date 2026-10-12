using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request body describing a single transform (operation) step on a create/update-pipeline request.
/// Maps onto <c>PipelineTransformConfiguration</c> (pipe.PipelineOperation), with <see cref="FieldMappings"/>,
/// <see cref="Aggregation"/>, <see cref="Lookup"/>, and <see cref="Calculation"/> persisting as its typed
/// cascade-child rows.
/// </summary>
/// <remarks>
/// Why: implements <see cref="ITransformOperationSpec"/> (FDW-556) so it flows directly into
/// <c>TransformTypeBase.MapSpecToConfiguration</c> — the per-option dispatch mechanism — without an
/// app-owned adapter type or a switch on <see cref="OperationType"/>.
/// </remarks>
public class CreatePipelineTransformRequest : ITransformOperationSpec, ITransformOrderingSpec, ITransformCheckingSpec, ITransformBranchingSpec, ITransformNotifyingSpec, ITransformUnionSpec, ITransformComputeSpec, IStatedMembersSpec
{
    /// <summary>
    /// Gets or sets the transform step name (display/identity within the pipeline).
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation/transform type (Map, Filter, Calculate, Lookup, Aggregate), resolved
    /// against <c>TransformTypes</c>.
    /// </summary>
    /// <remarks>
    /// Why: no silent "Map" default (FDW-556 Part 6.4) — the caller must name the operation explicitly;
    /// an absent/unresolvable value fails loud via <c>EtlLog.UnknownTransformType</c> at dispatch time
    /// rather than silently behaving as a field-mapping transform.
    /// </remarks>
    [Required]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution order of this transform within the pipeline.
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>Gets or sets whether the step runs. Enabled unless the request says otherwise.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the field mappings for this transform (used by Map transforms).
    /// </summary>
    public IList<CreatePipelineFieldMappingRequest> FieldMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the aggregation parameters (used by Aggregate transforms).
    /// </summary>
    public AggregationRequest? Aggregation { get; set; }

    /// <summary>
    /// Gets or sets the lookup parameters (used by Lookup transforms).
    /// </summary>
    public LookupRequest? Lookup { get; set; }

    /// <summary>
    /// Gets or sets the calculation parameters (used by Calculate transforms).
    /// </summary>
    public CalculationRequest? Calculation { get; set; }

    /// <summary>
    /// Gets or sets the filter expression (used by Filter transforms).
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>Gets or sets the fields a Sort transform orders by, most significant first.</summary>
    public IList<PipelineSortField>? Sort { get; set; }

    /// <summary>Gets or sets a LatestBy transform's parameters: key columns, order column, and which row of a key is kept.</summary>
    public PipelineLatestBy? LatestBy { get; set; }

    /// <summary>Gets or sets a Distinct transform's parameters: the key columns.</summary>
    public PipelineDistinct? Distinct { get; set; }

    /// <summary>Gets or sets the checks a Validate stage asks of every record, in order.</summary>
    public IList<PipelineCheck>? Checks { get; set; }

    /// <summary>Gets or sets what a Validate stage does with a record that fails a check: Reject or Fail.</summary>
    public string? OnFail { get; set; }

    /// <summary>
    /// Gets or sets the paths of a Branch stage, in the order they are tried: the first whose condition a record meets takes
    /// it. Refused on any other kind of stage.
    /// </summary>
    public IList<CreatePipelinePathRequest>? Paths { get; set; }

    /// <summary>
    /// Gets or sets what a Branch stage does with a record that matches no path: <c>Pass</c> or <c>Reject</c>. Required for a
    /// Branch stage, no default; refused on any other kind of stage.
    /// </summary>
    public string? Unmatched { get; set; }

    /// <summary>
    /// Gets or sets the notification a Notify stage sends: the rule, the run point and the message. Required for a Notify stage,
    /// no default; refused on any other kind of stage.
    /// </summary>
    public PipelineNotification? Notification { get; set; }

    /// <summary>
    /// Gets or sets the name of the data set a Union stage appends after the last record the pipeline's source produces; its
    /// <c>fieldMappings</c> say how that data set's fields become the stage's output fields. Required for a Union stage; refused
    /// on any other kind of stage.
    /// </summary>
    public string? UnionSource { get; set; }

    /// <summary>
    /// Gets or sets what a Compute stage applies: the Built transformation, by name or by id, and which field of the record
    /// feeds each of its inputs. Required for a Compute stage; refused on any other kind of stage.
    /// </summary>
    public PipelineCompute? Compute { get; set; }

    /// <inheritdoc />
    IReadOnlyList<IFieldMapping> ITransformOperationSpec.FieldMappings => [.. FieldMappings];

    /// <inheritdoc />
    // Why one list for three transforms: an Aggregate groups by it, and LatestBy and Distinct keep one row per its
    // values -- the key columns of each travel in the spec's GroupByFields (FDW-1109).
    IReadOnlyList<string> ITransformOperationSpec.GroupByFields => LatestBy?.KeyColumns?.ToList() ?? Distinct?.KeyColumns?.ToList() ?? Aggregation?.GroupByFields ?? [];

    IReadOnlyList<ITransformCheckSpec> ITransformCheckingSpec.Checks => Checks is null ? [] : [.. Checks.Select(check => new TransformCheckSpec(check.Name, check.Expression))];

    string? ITransformCheckingSpec.OnFail => OnFail;

    IReadOnlyList<ITransformPathSpec> ITransformBranchingSpec.Paths => Paths is null ? [] : [.. Paths.Cast<ITransformPathSpec>()];

    string? ITransformBranchingSpec.Unmatched => Unmatched;

    string? ITransformComputeSpec.ComputeTransformation => Compute?.Transformation;

    Guid? ITransformComputeSpec.ComputeTransformationId => Compute?.TransformationId;

    IReadOnlyList<ComputeInputSpec> ITransformComputeSpec.ComputeInputs =>
        Compute is null ? [] : [.. Compute.Inputs.Select(input => new ComputeInputSpec(input.InputName, input.SourceField))];

    IReadOnlyList<ComputeTableSpec> ITransformComputeSpec.ComputeTables =>
        Compute is null ? [] : [.. Compute.Tables.Select(table => new ComputeTableSpec(table.Input, table.From, table.Name))];

    IReadOnlyList<ComputeOutputSpec> ITransformComputeSpec.ComputeOutputs =>
        Compute is null ? [] : [.. Compute.Outputs.Select(output => new ComputeOutputSpec(output.Output, output.Route, output.Name))];

    IReadOnlyList<ComputeScalarSpec> ITransformComputeSpec.ComputeScalars =>
        Compute is null ? [] : [.. Compute.Scalars.Select(scalar => new ComputeScalarSpec(scalar.Input, scalar.Value, scalar.ValueType))];

    string? ITransformComputeSpec.ComputeOnRowFailure => Compute?.OnRowFailure;

    INotificationSpec? ITransformNotifyingSpec.Notification => Notification is null ? null : new TransformNotificationSpec(Notification.RuleName, Notification.RunPoint, Notification.Message);

    // Why the members are named here: a member stated on a stage of a kind that does not own it is refused, and it counts
    // as stated only when it is not null and not empty -- an absent member and an empty list are the same thing.
    IReadOnlyList<string> IStatedMembersSpec.StatedMembers => StatedMembers();

    IReadOnlyList<string> IStatedMembersSpec.StatedMappingMembers =>
        FieldMappings.Any(mapping => mapping.TransformationChain is { Count: > 0 }) ? ["transforms"] : [];

    private List<string> StatedMembers()
    {
        var stated = new List<string>();
        AddIf(stated, "fieldMappings", FieldMappings.Count > 0);
        AddIf(stated, "aggregation", Aggregation is not null);
        AddIf(stated, "lookup", Lookup is not null);
        AddIf(stated, "calculation", Calculation is not null);
        AddIf(stated, "filterExpression", !string.IsNullOrWhiteSpace(FilterExpression));
        AddIf(stated, "sort", Sort is { Count: > 0 });
        AddIf(stated, "latestBy", LatestBy is not null);
        AddIf(stated, "distinct", Distinct is not null);
        AddIf(stated, "checks", Checks is { Count: > 0 });
        AddIf(stated, "onFail", !string.IsNullOrEmpty(OnFail));
        AddIf(stated, "paths", Paths is { Count: > 0 });
        AddIf(stated, "unmatched", !string.IsNullOrEmpty(Unmatched));
        AddIf(stated, "notification", Notification is not null);
        AddIf(stated, "unionSource", !string.IsNullOrWhiteSpace(UnionSource));
        AddIf(stated, "compute", Compute is not null);
        return stated;
    }

    private static void AddIf(List<string> stated, string member, bool isStated)
    {
        if (isStated)
        {
            stated.Add(member);
        }
    }

    IReadOnlyList<ISortFieldSpec> ITransformOrderingSpec.SortFields => Sort is null ? [] : [.. Sort.Select(f => new SortFieldSpec(f.Field, f.Direction))];

    string? ITransformOrderingSpec.OrderField => LatestBy?.OrderColumn;

    string? ITransformOrderingSpec.KeepRule => LatestBy?.Keep;

    /// <inheritdoc />
    IReadOnlyList<IAggregationSpec> ITransformOperationSpec.Aggregations => Aggregation?.Aggregations ?? [];

    /// <inheritdoc />
    ILookupSpec? ITransformOperationSpec.Lookup => Lookup;

    /// <inheritdoc />
    IReadOnlyList<ICalculationSpec> ITransformOperationSpec.ComputedColumns => Calculation?.ComputedColumns ?? [];
}
