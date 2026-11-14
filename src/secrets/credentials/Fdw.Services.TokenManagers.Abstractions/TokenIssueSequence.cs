using System;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Reads the issue instant a bearer token carries in its <c>jti</c>: the token's issue sequence.
/// </summary>
/// <remarks>
/// <para>
/// The issuer mints every <c>jti</c> as an RFC 9562 version-7 UUID from the token's own issue instant
/// (<c>Guid.CreateVersion7(issuedAt)</c> in <c>JwtTokenIssuer</c>). Its first 48 bits are that instant in
/// Unix milliseconds, so the <c>jti</c> places the token in time a thousand times finer than <c>iat</c>,
/// which is whole seconds.
/// </para>
/// <para>
/// Why this exists (FDW-5655): a user-wide cut-off (<see cref="ITokenRevocationStore.RevokeAll"/>) is
/// written at full clock precision and compared with the issue instant the token presents. Presented as
/// <c>iat</c>, every token issued in second S read as issued at S.000, so a token minted at S.900 after a
/// cut-off at S.300 was refused as "issued before the sessions were ended". Read from the <c>jti</c>, it
/// is refused only when it was issued in the same millisecond as the cut-off, the boundary of the
/// precision the token carries, and refusing there is the safe side.
/// </para>
/// <para>
/// Decoded by hand rather than with <c>Guid.Version</c>, because this package targets netstandard2.0.
/// </para>
/// </remarks>
public static class TokenIssueSequence
{
    /// <summary>
    /// Reads the issue instant a <c>jti</c> carries, floored to the millisecond, the finest unit it carries.
    /// </summary>
    /// <param name="jti">The token's <c>jti</c> claim.</param>
    /// <param name="issuedAt">The issue instant, when the <c>jti</c> is a version-7 UUID.</param>
    /// <returns>
    /// <see langword="false"/> when the <c>jti</c> is not an RFC 9562 version-7 UUID: it carries no issue
    /// instant, and the caller refuses the token rather than reading one from anywhere else.
    /// </returns>
    public static bool TryReadIssuedAt(Guid jti, out DateTimeOffset issuedAt)
    {
        // Guid.ToByteArray writes its first three fields little-endian; the RFC octets are big-endian.
        // RFC octet 6 (version nibble) is byte 7 here, RFC octet 8 (variant bits) is byte 8.
        var bytes = jti.ToByteArray();
        if (bytes[7] >> 4 != 7 || (bytes[8] & 0xC0) != 0x80)
        {
            issuedAt = default;
            return false;
        }

        var unixMilliseconds =
            ((long)bytes[3] << 40) | ((long)bytes[2] << 32) | ((long)bytes[1] << 24)
            | ((long)bytes[0] << 16) | ((long)bytes[5] << 8) | bytes[4];

        issuedAt = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
        return true;
    }

    /// <summary>
    /// Returns whether the instant a <c>jti</c> carries falls within the whole second the same token's
    /// <c>iat</c> names.
    /// </summary>
    /// <param name="sequenceIssuedAt">The instant read by <see cref="TryReadIssuedAt"/>.</param>
    /// <param name="claimedIssuedAt">The token's <c>iat</c>, whole seconds.</param>
    /// <remarks>
    /// Both claims are signed and the issuer writes both from one instant, so they always agree on a token
    /// it minted. A token where they disagree did not take its <c>jti</c> from its issue instant, so the
    /// <c>jti</c> cannot order it against a cut-off.
    /// </remarks>
    public static bool AgreesWithIssuedAt(DateTimeOffset sequenceIssuedAt, DateTimeOffset claimedIssuedAt)
        => sequenceIssuedAt >= claimedIssuedAt && sequenceIssuedAt < claimedIssuedAt.AddSeconds(1);
}
