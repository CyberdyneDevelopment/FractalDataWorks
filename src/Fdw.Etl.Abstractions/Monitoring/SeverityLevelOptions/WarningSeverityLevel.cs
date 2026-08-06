using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.SeverityLevelOptions;

/// <summary>
/// Warning message.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SeverityLevels), "Warning", RestrictToCurrentCompilation = true)]
public sealed class WarningSeverityLevel : SeverityLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarningSeverityLevel"/> class.
    /// </summary>
    public WarningSeverityLevel() : base(2, "Warning", level: 2, logByDefault: true) { }
}
