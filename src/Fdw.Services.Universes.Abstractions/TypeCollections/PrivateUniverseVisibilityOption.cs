using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Invisible to non-members. Only a member can find it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseVisibilities), "Private")]
public sealed class PrivateUniverseVisibilityOption : UniverseVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="PrivateUniverseVisibilityOption"/> class.</summary>
    public PrivateUniverseVisibilityOption() : base("Private")
    {
    }
}
