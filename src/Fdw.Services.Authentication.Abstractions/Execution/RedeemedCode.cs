using System.Diagnostics.CodeAnalysis;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Execution;

/// <summary>What redeeming a one-time sign-in code yields: the token and the flow that issued it.</summary>
/// <remarks>
/// The flow travels with the token because it decides what the redeemer does with it (FDW-903): a
/// bearer flow's token is returned to the caller, and a session-delivered flow's product -- a session
/// id -- is set as the session cookie on the origin the code was redeemed from.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record RedeemedCode
{
    /// <summary>Gets the flow that issued the token.</summary>
    public required string FlowName { get; init; }

    /// <summary>Gets the token the flow issued.</summary>
    public required IssuedToken Token { get; init; }
}
