using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Error-level logging for failures.</summary>
[TypeOption(typeof(LogLevels), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorLogLevel : LogLevelBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorLogLevel"/>.</summary>
    public ErrorLogLevel() : base(4, "Error") { }
}
