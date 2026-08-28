using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Defines a single aggregate measure within a DataSet, including the group-by keys and
/// the aggregate function applied to an input field to produce a named output column.
/// Maps to the <c>data.DataSetAggregate</c> table in ConfigurationDb.
/// </summary>
/// <remarks>
/// <para>
/// <c>DataSetAggregateDefinition</c> stores group-by and measure definitions at the dataset level,
/// enabling reuse of aggregation logic across queries without repeating it in transformation
/// configurations.  For example, a "sales by state" dataset can carry two definitions:
/// <list type="bullet">
/// <item><description>TotalSales — SUM of Amount, grouped by State</description></item>
/// <item><description>TransactionCount — COUNT of Id, grouped by State</description></item>
/// </list>
/// </para>
/// <para>
/// <c>AggregateFunctionName</c> references an entry in the <c>AggregationFunctions</c>
/// TypeCollection by name.  The endpoint validates the name at create/update time and the
/// provider validates it again at load time via MessageLogging — it never silently falls back
/// to a default function.
/// </para>
/// <para>
/// <c>GroupByFieldNames</c> is stored as a comma-delimited string (e.g. <c>"State,Region"</c>).
/// Consumers must split and trim the value before use; empty elements after splitting are
/// treated as a load-time failure, not silently ignored.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataSetAggregate")]
public sealed partial class DataSetAggregateDefinition : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this aggregate definition.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name (computed; not a persisted column).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name (computed; not a persisted column).</summary>
    public string SectionName => "DataSetAggregates";

    /// <summary>Gets the service type domain.</summary>
    public string ServiceType => "DataSet";

    /// <summary>Gets the service option type discriminator (none for aggregate definitions).</summary>
    public string? ServiceOptionType => null;

    // ============================================================================
    // Parent foreign key
    // ============================================================================

    /// <summary>
    /// Gets or sets the parent DataSet logical identifier (FK to data.DataSet.Id).
    /// </summary>
    /// <remarks>
    /// Why: Denormalized on the child for efficient single-table query from the provider
    /// without a join through DataSet — mirrors the pattern on data.DataSetKeyField and
    /// data.DataSetSource which both carry DataSetId directly.
    /// </remarks>
    public Guid DataSetId { get; set; }

    // ============================================================================
    // Aggregate definition
    // ============================================================================

    /// <summary>
    /// Gets or sets the name of the output column produced by this aggregate
    /// (e.g. <c>"TotalSales"</c>, <c>"TransactionCount"</c>).
    /// </summary>
    /// <remarks>Must be unique within a DataSet. No default — a missing name fails at load time.</remarks>
    public string AggregateColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comma-delimited list of field names to group by when computing this aggregate
    /// (e.g. <c>"State"</c> or <c>"State,Region"</c>).
    /// </summary>
    /// <remarks>
    /// Stored as a single string to avoid a separate normalization table.
    /// Consumers split on <c>','</c> and trim whitespace. An empty element after splitting is a
    /// configuration error and must be reported via MessageLogging, not silently skipped.
    /// </remarks>
    public string GroupByFieldNames { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the aggregate function to apply
    /// (e.g. <c>"SUM"</c>, <c>"AVG"</c>, <c>"COUNT"</c>, <c>"MIN"</c>, <c>"MAX"</c>).
    /// </summary>
    /// <remarks>
    /// Resolved against the <c>AggregationFunctions</c> TypeCollection via <c>ByName()</c>.
    /// If the function name is not found the TypeCollection returns its <c>NotFound</c> sentinel;
    /// callers treat this as a failure and log via MessageLogging — it never defaults to a
    /// different function.
    /// </remarks>
    public string AggregateFunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the input field whose values are fed into the aggregate function
    /// (e.g. <c>"Amount"</c> for SUM, <c>"Id"</c> for COUNT).
    /// </summary>
    public string InputFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional human-facing label for this aggregate column.
    /// Falls back to <see cref="AggregateColumnName"/> when null.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets an optional description explaining the business meaning of this aggregate.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the display/execution order of this aggregate definition within its DataSet.
    /// Lower values are rendered and evaluated first.
    /// Unique per DataSet (enforced by UX_DataSetAggregate_DataSetId_Ordinal_Current).
    /// </summary>
    public int Ordinal { get; set; }

    // ============================================================================
    // Version-on-write / soft-delete
    // ============================================================================

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    // ============================================================================
    // Audit
    // ============================================================================

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
