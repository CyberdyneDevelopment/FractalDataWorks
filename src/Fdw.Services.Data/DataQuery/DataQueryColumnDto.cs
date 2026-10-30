using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A column of a data query's result, in the order the rows carry it.</summary>
public sealed class DataQueryColumnDto
{
    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field's data type name.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the field is part of the data set's key.</summary>
    public bool IsKey { get; set; }

    /// <summary>Gets or sets the field's role, when one is declared.</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets whether a caller may filter on this field; a filter on a field where this is false is refused.</summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// Gets or sets the operators a filter on this field may use, by name. Empty when the field is not filterable or is not
    /// yet typed, so a console shows "filter by" only for a field that has operators.
    /// </summary>
    public List<string> FilterOperators { get; set; } = [];
}
