namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// The recorded outcome of evaluating an <see cref="Fdw.Services.Abstractions.IRequestContext"/>
/// against a resolved canonical address. Carried back through the resolution chain so callers
/// (and audit) can see which policy made the decision and why.
/// </summary>
/// <remarks>
/// Created by an <c>IPathAuthorizationPolicy</c>. A successful decision carries the policy name
/// and the resolved address. A denied decision carries the policy name plus a reason; the
/// resolution result itself is then a failure with this decision attached for diagnostics.
/// </remarks>
public interface IPathAuthorizationDecision
{
    /// <summary>Whether the request context is authorized to access the resolved address.</summary>
    bool IsAllowed { get; }

    /// <summary>Name of the policy that produced this decision (matches a <c>PathAuthorizationPolicies</c> TypeOption).</summary>
    string PolicyName { get; }

    /// <summary>Human-readable reason for the decision; non-empty when denied.</summary>
    string Reason { get; }
}
