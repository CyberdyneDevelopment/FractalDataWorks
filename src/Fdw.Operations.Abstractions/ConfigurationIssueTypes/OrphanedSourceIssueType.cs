using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Issue type for orphaned source configuration issues.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationIssueTypes), "OrphanedSource", RestrictToCurrentCompilation = true)]
public sealed class OrphanedSourceIssueType : ConfigurationIssueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrphanedSourceIssueType"/> class.
    /// </summary>
    public OrphanedSourceIssueType() : base(2, "OrphanedSource", isHealable: true) { }
}
