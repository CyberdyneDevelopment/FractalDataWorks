using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Issue type for field count mismatch configuration issues.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationIssueTypes), "FieldCountMismatch", RestrictToCurrentCompilation = true)]
public sealed class FieldCountMismatchIssueType : ConfigurationIssueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldCountMismatchIssueType"/> class.
    /// </summary>
    public FieldCountMismatchIssueType() : base(7, "FieldCountMismatch", isHealable: true) { }
}
