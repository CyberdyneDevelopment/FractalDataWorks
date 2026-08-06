using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Debug-level logging for development.</summary>
[TypeOption(typeof(LogLevels), "Debug")]
[ExcludeFromCodeCoverage]
public sealed class DebugLogLevel : LogLevelBase
{
    /// <summary>Initializes a new instance of <see cref="DebugLogLevel"/>.</summary>
    public DebugLogLevel() : base(1, "Debug") { }
}
