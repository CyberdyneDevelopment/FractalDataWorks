namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>Where one table output of the Built transformation a Compute stage applies goes.</summary>
public sealed class PipelineComputeOutput
{
    /// <summary>Gets or sets the name of the table output the transformation declares.</summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>Gets or sets where it goes: Replace (its rows become the stage's rows; at most one output can) or Named (kept under <see cref="Name"/> for later stages).</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Gets or sets the name a Named route keeps the rows under; outputs that share a name add to each other. Null for Replace.</summary>
    public string? Name { get; set; }
}
