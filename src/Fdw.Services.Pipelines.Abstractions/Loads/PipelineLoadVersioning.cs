namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Versioned upsert: when a compare column differs, close the current row and insert a new one. There is no timestamp
/// field -- the close and open time is always the database clock (contract v4).
/// </summary>
public sealed class PipelineLoadVersioning
{
    /// <summary>Gets or sets the column set to the open time on the new row.</summary>
    public string EffectiveFromColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets the column set to the close time on the closed row.</summary>
    public string EffectiveToColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets the column cleared on close and set on the new row, or <see langword="null"/> when there is none.</summary>
    public string? CurrentFlagColumn { get; set; }

    /// <summary>Gets or sets the rule that leaves rows alone, or <see langword="null"/> when there is none.</summary>
    public PipelineLoadInsertOnlyRule? InsertOnlyRule { get; set; }
}
