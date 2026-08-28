using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>In use.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseStatuses), "Active")]
public sealed class ActiveUniverseStatusOption : UniverseStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ActiveUniverseStatusOption"/> class.</summary>
    public ActiveUniverseStatusOption() : base("Active")
    {
    }
}
