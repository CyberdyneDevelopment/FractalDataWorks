namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One row of the list of stored scripts.</summary>
public class ScriptSummaryPayload
{
    /// <summary>Gets or sets the script's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the script is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the name of the data store it runs on.</summary>
    public string Connection { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind: Query or NonQuery.</summary>
    public string Kind { get; set; } = string.Empty;
}
