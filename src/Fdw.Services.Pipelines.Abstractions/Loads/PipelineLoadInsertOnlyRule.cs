namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>Rows whose column holds the value are never modified by a versioned upsert (<c>insertOnlyRule</c>).</summary>
public sealed class PipelineLoadInsertOnlyRule
{
    /// <summary>Gets or sets the destination column the rule reads.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets the value that makes a row insert-only.</summary>
    public string Value { get; set; } = string.Empty;
}
