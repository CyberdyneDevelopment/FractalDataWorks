using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration for a filter condition stored with a DataSet definition.
/// These filters are applied automatically whenever the DataSet is queried.
/// Maps to the <c>cfg.DataSetFilter</c> child table (FK: DataSetId → data.DataSet.Id).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DataSetFilterConditionConfiguration
{
    /// <summary>Gets or sets the parent DataSet identifier (FK to data.DataSet.Id).</summary>
    public Guid DataSetId { get; set; }

    /// <summary>Gets or sets the field name to filter on.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter operator.
    /// Valid values: Equals, NotEquals, Contains, StartsWith, EndsWith,
    /// GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, IsNull, IsNotNull.
    /// </summary>
    public string Operator { get; set; } = "Equals";

    /// <summary>Gets or sets the comparison value. Null for IsNull/IsNotNull operators.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the data type of the field, used to build the correctly-typed filter at query time.</summary>
    public string DataType { get; set; } = "String";

    /// <summary>Gets or sets the ordinal position (order of application).</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }
}
