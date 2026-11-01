using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// One row of <c>auth.AuthorizationCode</c> (AuthDb): a token issued at the end of a redirect sign-in,
/// waiting for the browser to redeem it. <see cref="EncryptedToken"/> is the output of an
/// <c>EncryptCommand</c> under the issuing flow's key, never plaintext. <c>IssuedAt</c> is filled by
/// the database and is not carried here.
/// </summary>
/// <remarks>
/// Read back through <c>Query.From&lt;AuthorizationCodeEntity&gt;</c> in <c>DatabaseAuthorizationCodeStore</c>, which needs a generated
/// mapper; without <c>[GenerateMapper]</c> the insert succeeds and the read fails with "No POCO mapper
/// found" (FDW-918, the same defect SessionEntity had).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class AuthorizationCodeEntity
{
    /// <summary>Gets or sets the unkeyed SHA-256 hash of the code. Never the code itself.</summary>
    public byte[] CodeHash { get; set; } = [];

    /// <summary>Gets or sets the flow that issued the token; its key decrypts <see cref="EncryptedToken"/>.</summary>
    public string FlowName { get; set; } = string.Empty;

    /// <summary>Gets or sets the encrypted, serialized issued token.</summary>
    public byte[] EncryptedToken { get; set; } = [];

    /// <summary>Gets or sets when the code stops being redeemable.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets when the code was redeemed. Null until then.</summary>
    public DateTimeOffset? RedeemedAt { get; set; }
}
