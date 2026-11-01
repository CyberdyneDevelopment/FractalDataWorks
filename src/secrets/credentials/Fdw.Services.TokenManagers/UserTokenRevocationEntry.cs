using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// One row of <c>auth.UserTokenRevocation</c> (AuthDb): every token issued to <see cref="UserId"/> at
/// or before <see cref="RevokedAt"/> is no longer accepted, whatever its <c>jti</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class UserTokenRevocationEntry
{
    /// <summary>Gets or sets the user whose sessions were ended — the tokens' <c>sub</c> claim.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the cut-off: a token issued at or before this moment is rejected.</summary>
    public DateTimeOffset RevokedAt { get; set; }
}
