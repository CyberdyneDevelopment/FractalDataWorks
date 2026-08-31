using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Appears to everyone in the tenant, and its contents are readable without joining.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseVisibilities), "Open")]
public sealed class OpenUniverseVisibilityOption : UniverseVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="OpenUniverseVisibilityOption"/> class.</summary>
    public OpenUniverseVisibilityOption() : base("Open")
    {
    }
}
