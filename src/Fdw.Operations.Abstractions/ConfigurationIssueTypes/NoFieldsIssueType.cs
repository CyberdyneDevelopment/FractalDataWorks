using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Issue type for no fields configuration issues.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationIssueTypes), "NoFields", RestrictToCurrentCompilation = true)]
public sealed class NoFieldsIssueType : ConfigurationIssueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoFieldsIssueType"/> class.
    /// </summary>
    public NoFieldsIssueType() : base(5, "NoFields", isHealable: true) { }
}
