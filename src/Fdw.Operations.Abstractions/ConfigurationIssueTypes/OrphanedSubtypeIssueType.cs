using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Issue type for orphaned subtype configuration issues.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationIssueTypes), "OrphanedSubtype", RestrictToCurrentCompilation = true)]
public sealed class OrphanedSubtypeIssueType : ConfigurationIssueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrphanedSubtypeIssueType"/> class.
    /// </summary>
    public OrphanedSubtypeIssueType() : base(1, "OrphanedSubtype", isHealable: true) { }
}
