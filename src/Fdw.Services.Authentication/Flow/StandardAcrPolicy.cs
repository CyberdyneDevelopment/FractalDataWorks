using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Steps;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Turns proved methods into an assurance level by counting distinct factor kinds.
/// </summary>
/// <remarks>
/// <para>
/// NIST SP 800-63 defines assurance by how many independent factors were used, not how many times
/// someone authenticated. Two passwords are one factor twice, so the count here is of distinct
/// <em>kinds</em> — something known, something held, something inherent.
/// </para>
/// <para>
/// A deployment that recognises different rungs replaces this. What must not move is where it lives:
/// the runner owns the policy, so no step can raise the level it reports.
/// </para>
/// </remarks>
public sealed class StandardAcrPolicy : IAcrPolicy
{
    /// <summary>The level reached by a single factor.</summary>
    public const string SingleFactor = "urn:fdw:acr:single-factor";

    /// <summary>The level reached by two or more distinct factor kinds.</summary>
    public const string MultiFactor = "urn:fdw:acr:multi-factor";

    // RFC 8176 method values, grouped by the kind of factor each represents. A method not listed
    // counts as its own kind rather than being ignored: an unrecognised proof is still a proof, and
    // silently discarding it would understate assurance rather than overstate it.
    private static readonly Dictionary<string, string> FactorKinds = new(StringComparer.Ordinal)
    {
        ["pwd"] = "knowledge",
        ["pin"] = "knowledge",
        ["otp"] = "possession",
        ["sms"] = "possession",
        ["hwk"] = "possession",
        ["swk"] = "possession",
        ["mfa"] = "possession",
        ["fpt"] = "inherence",
        ["face"] = "inherence",
        ["iris"] = "inherence",
        ["vbm"] = "inherence",
        ["retina"] = "inherence",
    };

    /// <inheritdoc />
    public string? Evaluate(IReadOnlyList<string> achievedMethods)
    {
        ArgumentNullException.ThrowIfNull(achievedMethods);

        return achievedMethods
            .Select(m => FactorKinds.TryGetValue(m, out var kind) ? kind : m)
            .Distinct(StringComparer.Ordinal)
            .Count() switch
        {
            0 => null,
            1 => SingleFactor,
            _ => MultiFactor,
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// A flow demanding nothing is satisfied by anything, including nothing — the terminal check
    /// separately refuses a flow that proved no one, so this cannot be the only gate.
    /// </remarks>
    public bool Meets(string? achieved, string? required) => required switch
    {
        null => true,
        SingleFactor => achieved is SingleFactor or MultiFactor,
        MultiFactor => achieved == MultiFactor,
        _ => string.Equals(achieved, required, StringComparison.Ordinal),
    };
}
