using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Response DTO naming a data set field and whether API callers may filter on it.
/// </summary>
public class FilterableFieldResponse
{
    /// <summary>Gets or sets the field's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether API callers may filter on the field.</summary>
    public bool IsFilterable { get; set; }
}
