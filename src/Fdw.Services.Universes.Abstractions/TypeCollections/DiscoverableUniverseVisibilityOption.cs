using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Appears in search and listings, so someone can ask to join. Its contents stay closed.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseVisibilities), "Discoverable")]
public sealed class DiscoverableUniverseVisibilityOption : UniverseVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="DiscoverableUniverseVisibilityOption"/> class.</summary>
    public DiscoverableUniverseVisibilityOption() : base("Discoverable")
    {
    }
}
