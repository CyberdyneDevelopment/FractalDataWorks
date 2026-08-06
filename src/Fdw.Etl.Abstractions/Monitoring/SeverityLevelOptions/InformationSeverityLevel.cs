using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.SeverityLevelOptions;

/// <summary>
/// Informational message.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SeverityLevels), "Information", RestrictToCurrentCompilation = true)]
public sealed class InformationSeverityLevel : SeverityLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InformationSeverityLevel"/> class.
    /// </summary>
    public InformationSeverityLevel() : base(1, "Information", level: 1, logByDefault: true) { }
}
