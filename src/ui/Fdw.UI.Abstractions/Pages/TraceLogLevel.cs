using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Trace-level logging for detailed debugging.</summary>
[TypeOption(typeof(LogLevels), "Trace")]
[ExcludeFromCodeCoverage]
public sealed class TraceLogLevel : LogLevelBase
{
    /// <summary>Initializes a new instance of <see cref="TraceLogLevel"/>.</summary>
    public TraceLogLevel() : base(0, "Trace") { }
}
