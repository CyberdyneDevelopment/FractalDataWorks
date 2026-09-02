using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// One row of <c>auth.RevokedAccessToken</c> (AuthDb): a token this host will no longer accept,
/// keyed by the <c>jti</c> claim it was minted with.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RevokedAccessTokenEntry
{
    /// <summary>Gets or sets the revoked token's <c>jti</c> claim.</summary>
    public Guid Jti { get; set; }

    /// <summary>Gets or sets when this row was written. DB-defaulted; not set on insert.</summary>
    public DateTimeOffset RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets when the token itself would have expired anyway. Not read at check time — the
    /// token's own <c>exp</c> claim already stops it being presented once it lapses — but kept so a
    /// housekeeping job can prune rows nobody can present anymore.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
