using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Finished. Retained for reference, and what the universe owns goes with it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseStatuses), "Archived")]
public sealed class ArchivedUniverseStatusOption : UniverseStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ArchivedUniverseStatusOption"/> class.</summary>
    public ArchivedUniverseStatusOption() : base("Archived")
    {
    }
}
