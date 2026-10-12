using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// The single column an atomic redeem touches on <c>auth.AuthorizationCode</c>. Narrower than
/// <see cref="AuthorizationCodeEntity"/> on purpose, as <see cref="AuthenticationExecutionConsumeUpdate"/>
/// is: the WHERE clause guarding the redeem is what makes it atomic, and nothing else on the row can be
/// overwritten.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AuthorizationCodeRedeemUpdate
{
    /// <summary>Gets or sets when the code was redeemed.</summary>
    public DateTimeOffset RedeemedAt { get; set; }
}
