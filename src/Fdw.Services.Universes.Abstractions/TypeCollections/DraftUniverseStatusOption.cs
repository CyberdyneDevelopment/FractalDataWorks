using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Being set up; not yet in use.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseStatuses), "Draft")]
public sealed class DraftUniverseStatusOption : UniverseStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="DraftUniverseStatusOption"/> class.</summary>
    public DraftUniverseStatusOption() : base("Draft")
    {
    }
}
