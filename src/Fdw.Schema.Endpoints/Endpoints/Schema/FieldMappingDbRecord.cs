using Fdw.Data;
using System;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Internal entity for DataSetFieldMapping table.
/// </summary>
// Why [GenerateMapper]: a query materialises through a generated POCO mapper, and the generator
// emits one only for a type carrying this attribute. Without it the read threw "No POCO mapper
// found" on every call — and the caller was handed 200 with an empty list, so a mapping that
// exists in the database read as a dataset that has none.
[GenerateMapper]
public partial class FieldMappingDbRecord
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the DataSet source identifier.</summary>
    public Guid DataSetSourceId { get; set; }
    /// <summary>Gets or sets the logical field name (DataSet/target field).</summary>
    public string LogicalFieldName { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical field name (source field).</summary>
    public string PhysicalFieldName { get; set; } = string.Empty;
    /// <summary>Gets or sets the transform expression.</summary>
    public string? TransformExpression { get; set; }
    /// <summary>Gets or sets whether this mapping is the current version.</summary>
    public bool IsCurrent { get; set; }
    /// <summary>Gets or sets whether this mapping has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }
}