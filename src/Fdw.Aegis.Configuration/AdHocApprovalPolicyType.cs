using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// The ad-hoc policy kind: every invocation requires a fresh verdict rather than a standing
/// pre-approval.
/// </summary>
[TypeOption(typeof(ApprovalPolicyTypes), "AdHoc", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AdHocApprovalPolicyType : ApprovalPolicyTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdHocApprovalPolicyType"/> class.
    /// </summary>
    public AdHocApprovalPolicyType()
        : base(id: 2, name: "AdHoc", configurationType: typeof(AdHocCommandConfiguration))
    {
    }
}
