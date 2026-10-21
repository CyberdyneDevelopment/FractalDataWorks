namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A column of the rows a query answered.</summary>
public class ScriptColumnPayload
{
    /// <summary>Gets or sets the column's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the .NET type name of the first value the column held; null when every value was null.</summary>
    public string? Type { get; set; }
}
