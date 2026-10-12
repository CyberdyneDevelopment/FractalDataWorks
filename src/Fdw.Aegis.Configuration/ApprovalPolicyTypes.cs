using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// Registry of Aegis command approval-policy kinds (PreApproved/AdHoc). Pure type collection with
/// no DI orchestration — a policy kind is config-data, not a resolved service.
/// </summary>
/// <remarks>
/// Uses <c>[MutableTypeCollection]</c> so a future policy kind (e.g. a Phase-4 auto-approve-agent
/// binding) can register from its own assembly without a framework change, mirroring
/// <c>DataSetTypes</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ApprovalPolicyTypeBase), typeof(IApprovalPolicyType), typeof(ApprovalPolicyTypes))]
public abstract partial class ApprovalPolicyTypes : TypeCollectionBase<ApprovalPolicyTypeBase, IApprovalPolicyType>
{
}
