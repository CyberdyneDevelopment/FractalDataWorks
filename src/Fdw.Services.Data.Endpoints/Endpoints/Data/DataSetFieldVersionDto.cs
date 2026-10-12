using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>One stored version of a data set field.</summary>
public class DataSetFieldVersionDto
{
    /// <summary>Gets or sets the version number, 1 for the first one written.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets whether this is the version in effect now.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this version records the field's deletion.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets when this version was written.</summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>Gets or sets the database identity that wrote this version.</summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the user on whose behalf this version was written.</summary>
    public string ChangedOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the properties that differ from the previous version. Empty for version 1.</summary>
    public IReadOnlyList<string> Changes { get; set; } = [];

    /// <summary>Gets or sets the field's name in this version. Differs from the current name across a rename.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field's type name in this version.</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description in this version.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the semantic role in this version.</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets whether the field was a key in this version.</summary>
    public bool IsKey { get; set; }

    /// <summary>Gets or sets whether the field allowed null in this version.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets whether the field was required in this version.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets whether the field was indexed in this version.</summary>
    public bool IsIndexed { get; set; }

    /// <summary>Gets or sets the maximum length in this version.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets the default value in this version.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Gets or sets the position in this version.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the calculation the field was computed by in this version.</summary>
    public string? CalculationName { get; set; }
}
