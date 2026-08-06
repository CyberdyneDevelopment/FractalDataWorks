using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// The pre-approved policy kind: the command's parameter allow-list is the whole approval
/// contract — no per-invocation human decision is required once parameters validate.
/// </summary>
[TypeOption(typeof(ApprovalPolicyTypes), "PreApproved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PreApprovedApprovalPolicyType : ApprovalPolicyTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreApprovedApprovalPolicyType"/> class.
    /// </summary>
    public PreApprovedApprovalPolicyType()
        : base(id: 1, name: "PreApproved", configurationType: typeof(PreApprovedCommandConfiguration))
    {
    }
}
