using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Warning-level logging for potential issues.</summary>
[TypeOption(typeof(LogLevels), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningLogLevel : LogLevelBase
{
    /// <summary>Initializes a new instance of <see cref="WarningLogLevel"/>.</summary>
    public WarningLogLevel() : base(3, "Warning") { }
}
