using Fdw.Collections;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// A closed disposition an <see cref="IApprovalPolicyEvaluator"/> can render for an
/// <see cref="ApprovalRequest"/>. Behavior (whether the disposition is final, whether it permits
/// <c>Aegis.Injector</c> to proceed) lives on the option — never a switch at the call site.
/// </summary>
/// <remarks>
/// This is a CLOSED collection (<c>[TypeCollection]</c>, not <c>[MutableTypeCollection]</c>) — the
/// verdict vocabulary is fixed (Approve/Deny/Abstain/Pending); it is not an extension point for
/// downstream assemblies the way e.g. <c>IIsolationLevel</c> is.
/// </remarks>
public interface IVerdictDisposition : ITypeOption<int, VerdictDispositionBase>
{
    /// <summary>
    /// Gets a value indicating whether this disposition is a final answer (Approve/Deny) rather
    /// than an intermediate state (Abstain/Pending) still awaiting a further decision.
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets a value indicating whether this disposition permits <c>Aegis.Injector</c> to resolve the
    /// secret and inject it below the boundary. Fail-closed: only <c>Approve</c> returns
    /// <see langword="true"/> — every other disposition, including states not yet defined, must not.
    /// </summary>
    bool AllowsInjection { get; }
}
