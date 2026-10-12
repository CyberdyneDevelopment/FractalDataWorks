using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The options of a load: the columns that match, compare and are written, and the versioning and soft-delete options
/// (<c>loadOptions</c>, and the shared part of a load step's <c>options</c>).
/// </summary>
public class PipelineLoadOptions
{
    /// <summary>Gets or sets the destination columns that match a row.</summary>
    public IList<string> KeyColumns { get; set; } = [];

    /// <summary>Gets or sets the columns that make a matched row count as changed; empty means every matched row does.</summary>
    public IList<string> CompareColumns { get; set; } = [];

    /// <summary>Gets or sets the destination columns written.</summary>
    public IList<string> WriteColumns { get; set; } = [];

    /// <summary>Gets or sets the versioning option, or <see langword="null"/> when the load is not versioned.</summary>
    public PipelineLoadVersioning? Versioning { get; set; }

    /// <summary>Gets or sets the soft-delete option, or <see langword="null"/> when rows are not soft-deleted.</summary>
    public PipelineLoadSoftDelete? SoftDelete { get; set; }
}
