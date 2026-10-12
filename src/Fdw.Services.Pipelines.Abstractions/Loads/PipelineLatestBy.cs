using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A LatestBy transform's parameters (<c>latestBy</c>): one row per key, the first or last by the order column.</summary>
public sealed class PipelineLatestBy
{
    /// <summary>Gets or sets the key fields: one row is kept per key.</summary>
    public IList<string> KeyColumns { get; set; } = [];

    /// <summary>Gets or sets the field the rows of a key are ordered by.</summary>
    public string OrderColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets which row of a key is kept, First or Last.</summary>
    public string Keep { get; set; } = string.Empty;
}
