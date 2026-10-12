namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// Concrete <see cref="IPathAuthorizationDecision"/> produced by a path authorization policy.
/// Stateless record-style value.
/// </summary>
public sealed class PathAuthorizationDecision : IPathAuthorizationDecision
{
    /// <summary>Initializes an allow decision.</summary>
    public static PathAuthorizationDecision Allow(string policyName) =>
        new(true, policyName, string.Empty);

    /// <summary>Initializes a deny decision with a reason.</summary>
    public static PathAuthorizationDecision Deny(string policyName, string reason) =>
        new(false, policyName, reason);

    private PathAuthorizationDecision(bool isAllowed, string policyName, string reason)
    {
        IsAllowed = isAllowed;
        PolicyName = policyName;
        Reason = reason;
    }

    /// <inheritdoc />
    public bool IsAllowed { get; }

    /// <inheritdoc />
    public string PolicyName { get; }

    /// <inheritdoc />
    public string Reason { get; }
}
