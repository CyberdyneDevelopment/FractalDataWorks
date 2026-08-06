using System;
using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Represents a field within a DataSet, including its type and constraints.
/// </summary>
public sealed class DataSetFieldPayload : IDataSetField
{
    /// <summary>
    /// Gets or sets the unique identifier for the field.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the field.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the data type of the field.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the field is part of the primary key.
    /// </summary>
    public bool IsKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field is required (non-nullable).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field is indexed.
    /// </summary>
    public bool IsIndexed { get; set; }

    /// <summary>
    /// Gets or sets the maximum length for string fields, or null if not applicable.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the default value expression for the field, or null if none.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field value is derived from a calculation.
    /// </summary>
    public bool IsCalculated { get; set; }

    /// <summary>
    /// Gets or sets the semantic role of the field, or null if none assigned.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position of the field within the DataSet.
    /// </summary>
    public int Ordinal { get; set; }
}
