using System;
using Fdw.Data;

namespace Fdw.Data.DataSets;

/// <summary>
/// Maps to <c>data.DataSetField</c> — field definitions within a DataSet.
/// Child of <see cref="Abstractions.DataSetConfiguration"/> via DataSetId FK.
/// </summary>
[GenerateMapper]
public sealed partial class DataSetFieldConfiguration
{

    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent DataSet identifier.</summary>
    public Guid DataSetId { get; set; }


    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type name (e.g., "int", "nvarchar").</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional role of this field (e.g., "Key", "Measure").</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets whether the field allows null values.</summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>Gets or sets whether the field is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets the optional maximum length for string/binary types.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets the ordinal position of the field within the DataSet.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
