using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>Normal priority.</summary>
[TypeOption(typeof(CachePriorities), "Normal")]
[ExcludeFromCodeCoverage]
public sealed class NormalCachePriority : CachePriorityBase
{
    /// <summary>Initializes a new instance of <see cref="NormalCachePriority"/>.</summary>
    public NormalCachePriority() : base(1, "Normal") { }
}
