using System;
using System.Collections.Generic;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// A foreign authority this host will exchange a token from, and the terms it is trusted on.
/// </summary>
/// <remarks>
/// Which authority a caller may be exchanged FROM, which is a different decision from which issuers
/// a token may arrive on: a host can accept an authority's tokens directly without letting them be
/// traded for its own. The authority itself is a deployment fact, so the implementation lives in a
/// reference package and only this contract is framework.
/// </remarks>
public interface IForeignAuthorityConfiguration
{
    /// <summary>Gets the issuer a token must name in its <c>iss</c> claim.</summary>
    string Issuer { get; }

    /// <summary>Gets where the authority publishes the keys it signs with.</summary>
    Uri JwksUri { get; }

    /// <summary>Gets the audiences a token may name.</summary>
    IReadOnlyList<string> ValidAudiences { get; }

    /// <summary>Gets the signing algorithms accepted.</summary>
    /// <remarks>
    /// The host's decision rather than the token's: one that chooses its own algorithm can choose
    /// one its key trivially satisfies.
    /// </remarks>
    IReadOnlyList<string> ValidAlgorithms { get; }

    /// <summary>Gets the authentication methods this authority may assert.</summary>
    /// <remarks>
    /// RFC 8176 values, and the ceiling on what a token from this authority can prove: the runner
    /// keeps what a step observed only where the step also declared it may assert it, so widening
    /// this widens what a foreign authority is trusted to have actually verified.
    /// </remarks>
    IReadOnlyList<string> AssertableMethods { get; }

    /// <summary>Gets the tolerated clock difference between here and the authority.</summary>
    TimeSpan ClockSkew { get; }
}
