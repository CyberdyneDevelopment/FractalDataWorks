using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// One condition of a data set's own filter, applied with every read of the data set: a stored field, an operator and a value of the
/// field's data type (FDW-4997). Maps to the <c>data.DataSetFilter</c> table in ConfigurationDb.
/// </summary>
/// <remarks>
/// <para>
/// A composed child of a Simple, Compound or Federated data set, saved and loaded with it through <c>DataSetFilterConfigurationCommand</c>
/// the same way <see cref="DataSetAggregateConfiguration"/> and <c>DataSetJoinConfiguration</c> are. The row's link to its data set (<c>SimpleDataSetConfigurationRowId</c>,
/// <c>CompoundDataSetConfigurationRowId</c> or <c>FederatedDataSetConfigurationRowId</c>, whichever kind owns it) is filled by the provider
/// from the foreign key its container lists, so it is not a property here.
/// </para>
/// <para>
/// A data set's conditions are ANDed, in <see cref="Ordinal"/> order, with whatever the caller filters by. <see cref="FieldName"/> names a
/// stored field of the data set by its logical name, <see cref="Operator"/> a <c>FilterOperators</c> name and <see cref="DataType"/> a
/// <c>DataTypes</c> name; the rules (<c>DataSetFilters</c>) are checked when the data set is saved and again when it is read.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataSetFilter")]
public sealed partial class DataSetFilterConditionConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the logical identifier for this condition.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name (computed; not a persisted column).</summary>
    public string Name { get; set; } = string.Empty;

    // ============================================================================
    // Condition
    // ============================================================================

    /// <summary>Gets or sets the logical name of the stored field the condition compares.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the operator: a <c>FilterOperators</c> name such as <c>Equal</c>, <c>Contains</c> or <c>IsNull</c>.</summary>
    /// <remarks>Why no default here: the authored value always flows through from the create/update request.</remarks>
    public string Operator { get; set; } = string.Empty;

    /// <summary>Gets or sets the value compared with, as text in the field's data type; null for an operator that takes none.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the data type <see cref="Value"/> is read as: a <c>DataTypes</c> name such as <c>String</c> or <c>Int32</c>.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the condition's position among the data set's conditions.</summary>
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
