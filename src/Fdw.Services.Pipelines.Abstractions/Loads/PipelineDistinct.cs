using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A Distinct transform's parameters (<c>distinct</c>): one row per key, with no ordering.</summary>
public sealed class PipelineDistinct
{
    /// <summary>Gets or sets the key fields.</summary>
    public IList<string> KeyColumns { get; set; } = [];
}
