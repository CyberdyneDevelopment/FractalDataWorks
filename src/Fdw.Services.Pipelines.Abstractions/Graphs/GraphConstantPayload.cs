using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A constant of a field mapping (FDW-1396): the type that reads it and its value as text.</summary>
public class GraphConstantPayload
{
    /// <summary>Gets or sets the data type name that reads the value.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as text; none when the constant is null.</summary>
    public string? Value { get; set; }
}
