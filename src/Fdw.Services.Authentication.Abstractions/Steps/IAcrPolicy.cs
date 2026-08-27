using System.Collections.Generic;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Turns the set of authentication methods actually proved into an assurance level.
/// </summary>
/// <remarks>
/// Owned by the runner rather than by any step, so no step can raise the level it reports. What a
/// given set of methods is worth is a deployment's decision — NIST SP 800-63 defines the ladder,
/// not which rungs a particular platform recognises.
/// </remarks>
public interface IAcrPolicy
{
    /// <summary>Returns the level <paramref name="achievedMethods"/> amounts to.</summary>
    /// <param name="achievedMethods">RFC 8176 method values proved during this flow.</param>
    string? Evaluate(IReadOnlyList<string> achievedMethods);

    /// <summary>Returns whether <paramref name="achieved"/> satisfies <paramref name="required"/>.</summary>
    /// <param name="achieved">The level reached.</param>
    /// <param name="required">The level the flow demands, or null if it demands none.</param>
    bool Meets(string? achieved, string? required);
}
