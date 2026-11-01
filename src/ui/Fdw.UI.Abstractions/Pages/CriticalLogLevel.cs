using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Critical-level logging for severe failures.</summary>
[TypeOption(typeof(LogLevels), "Critical")]
[ExcludeFromCodeCoverage]
public sealed class CriticalLogLevel : LogLevelBase
{
    /// <summary>Initializes a new instance of <see cref="CriticalLogLevel"/>.</summary>
    public CriticalLogLevel() : base(5, "Critical") { }
}
