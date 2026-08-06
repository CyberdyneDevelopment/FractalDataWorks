using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Issue type for orphaned field configuration issues.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationIssueTypes), "OrphanedField", RestrictToCurrentCompilation = true)]
public sealed class OrphanedFieldIssueType : ConfigurationIssueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrphanedFieldIssueType"/> class.
    /// </summary>
    public OrphanedFieldIssueType() : base(3, "OrphanedField", isHealable: true) { }
}
