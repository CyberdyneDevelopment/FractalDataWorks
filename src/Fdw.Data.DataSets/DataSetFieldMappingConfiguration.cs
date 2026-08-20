using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets;

/// <summary>
/// Configuration for a single logical-to-physical field mapping under a DataSet source.
/// Maps to <c>data.DataSetFieldMapping</c> in ConfigurationDb.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataSet",
    ServiceType = "DataSetFieldMapping")]
public sealed partial class DataSetFieldMappingConfiguration : IGenericConfiguration
{

    /// <summary>Gets or sets the durable logical identity for this field mapping.</summary>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string SectionName => "DataSetFieldMappings";

    /// <inheritdoc/>
    public string ServiceType => "DataSet";

    /// <inheritdoc/>
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the name this mapping is known by within its source.</summary>
    /// <remarks>
    /// A mapping is identified by the field it fills, so the name is the logical field name. The
    /// interface requires one because every configuration the cascade writes is addressable; this
    /// is not a second identifier.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parent DataSet source logical identifier (FK to data.DataSetSource.Id).</summary>
    public Guid DataSetSourceId { get; set; }


    /// <summary>
    /// Gets or sets the logical field name from the DataSet schema.
    /// </summary>
    /// <value>The field name as defined in the DataSet (e.g., "CustomerId").</value>
    public string LogicalFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of source this mapping binds to.
    /// One of: <c>"DataStore"</c> (physical column on the parent DataSetSource's table — default),
    /// <c>"DataSet"</c> (a field on another DataSet via SourceDataSetId — compound/federated),
    /// <c>"Calculation"</c> (a CalculationEntity produces the value — derived field).
    /// </summary>
    // Why: A logical DataSet field can resolve from any of three kinds of upstream, not just
    // physical source columns. A single LogicalFieldName may have multiple mapping rows (multi-source
    // binding); when mappings share a LogicalFieldName, TransformationTypeName on each row declares
    // how those inputs combine.
    public string SourceKind { get; set; } = "DataStore";

    /// <summary>
    /// Gets or sets the physical field name in the source (used when SourceKind='DataStore').
    /// Null when SourceKind is 'DataSet' (resolved via SourceDataSetId on parent DataSetSource) or 'Calculation'.
    /// </summary>
    /// <value>The column/property name in the physical source (e.g., "customer_id", "CUST_ID").</value>
    public string? PhysicalFieldName { get; set; }




    /// <summary>
    /// Gets or sets the name of a <c>TransformationTypes</c> entry that combines inputs when
    /// multiple mappings target the same LogicalFieldName (e.g., <c>"Calculation"</c>,
    /// <c>"Aggregation"</c>, <c>"Lookup"</c>, <c>"Pivot"</c>, <c>"DataCleaning"</c>).
    /// Null means pass-through from a single source.
    /// </summary>
    public string? TransformationTypeName { get; set; }

    /// <summary>
    /// Gets or sets an optional free-form transformation expression.
    /// Prefer <see cref="TransformationTypeName"/> when the operation maps to a known TypeCollection entry.
    /// </summary>
    public string? TransformExpression { get; set; }

    /// <summary>Gets or sets the ordinal position for ordering mappings within a source.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the tenant identifier for row-level security.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the visibility group identifier for row-level security.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets the timestamp when the record was created in this system.</summary>
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
