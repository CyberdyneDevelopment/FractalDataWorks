namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>Sets a flag instead of deleting a row (<c>softDelete</c>).</summary>
public sealed class PipelineLoadSoftDelete
{
    /// <summary>Gets or sets the column set true on a soft-deleted row.</summary>
    public string DeletedFlagColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets the column set to the database clock, or <see langword="null"/> when there is none.</summary>
    public string? DeletedAtColumn { get; set; }

    /// <summary>Gets or sets the mode name, from the SoftDeleteModes type collection (MarkerMatches or AbsentFromSource).</summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>Gets or sets the marker MarkerMatches reads; required for MarkerMatches, refused for AbsentFromSource.</summary>
    public PipelineLoadMarker? Marker { get; set; }
}
