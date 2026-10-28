using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Claims.Abstractions;

/// <summary>
/// A known FDW JWT claim type. Implemented by <see cref="ClaimDefinitionBase"/> and every concrete
/// claim, enabling source-generated discovery via the <c>ClaimDefinitions</c> TypeCollection so any
/// assembly can contribute new claims with their own baking metadata (no FDW edit required).
/// </summary>
public interface IClaimDefinition : ITypeOption<int, IClaimDefinition>
{
    /// <summary>
    /// When true, the claim is serialized as a JSON array even for a single value
    /// (e.g. <c>roles</c>) so clients never have to branch on scalar-vs-array.
    /// </summary>
    bool IsArray { get; }

    /// <summary>
    /// When true, the claim is the platform's statement about a session it created (who is acting, with which role, for which agent, in
    /// which tenant tier). No other issuer has standing to make it, so a token from another issuer can never be configured to carry it
    /// onto a principal (FDW-2933). A claim states this; it has no default.
    /// </summary>
    bool IsSessionBound { get; }

    /// <summary>
    /// The token destination name(s) the claim is written to (see <see cref="TokenDestinations"/>).
    /// </summary>
    IReadOnlyList<string> Destinations { get; }
}
