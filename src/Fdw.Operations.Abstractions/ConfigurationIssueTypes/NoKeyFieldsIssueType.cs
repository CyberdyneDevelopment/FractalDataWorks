using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Issue type for no key fields configuration issues.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationIssueTypes), "NoKeyFields", RestrictToCurrentCompilation = true)]
public sealed class NoKeyFieldsIssueType : ConfigurationIssueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoKeyFieldsIssueType"/> class.
    /// </summary>
    public NoKeyFieldsIssueType() : base(6, "NoKeyFields", isHealable: true) { }
}
