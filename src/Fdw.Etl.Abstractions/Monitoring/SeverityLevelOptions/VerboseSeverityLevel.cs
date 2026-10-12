using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.SeverityLevelOptions;

/// <summary>
/// Verbose information.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SeverityLevels), "Verbose", RestrictToCurrentCompilation = true)]
public sealed class VerboseSeverityLevel : SeverityLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerboseSeverityLevel"/> class.
    /// </summary>
    public VerboseSeverityLevel() : base(0, "Verbose", level: 0, logByDefault: false) { }
}
