using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Temporarily not in use, but not archived.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseStatuses), "Paused")]
public sealed class PausedUniverseStatusOption : UniverseStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="PausedUniverseStatusOption"/> class.</summary>
    public PausedUniverseStatusOption() : base("Paused")
    {
    }
}
