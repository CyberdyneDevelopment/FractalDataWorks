using System;
using System.Collections.Generic;

namespace Fdw.Protocols.Tds;

/// <summary>
/// Builds this server's answer to a pre-login message (MS-TDS 2.2.6.5).
/// </summary>
/// <remarks>
/// It answers VERSION, ENCRYPTION, INSTOPT, THREADID and MARS, the options a SQL Server answers, plus
/// FEDAUTHREQUIRED when the client sent it. Multiple active result sets are always answered off: one
/// request runs at a time on a connection.
/// </remarks>
public static class PreLoginResponse
{
    /// <summary>Builds the pre-login answer.</summary>
    /// <param name="version">The version this server reports.</param>
    /// <param name="encryption">The ENCRYPTION value this server answers.</param>
    /// <param name="answerFederatedAuthentication">Whether the client sent the federated-authentication option.</param>
    /// <returns>The message payload.</returns>
    public static byte[] Build(TdsServerVersion version, byte encryption, bool answerFederatedAuthentication)
    {
        ArgumentNullException.ThrowIfNull(version);

        var options = new List<(byte Token, byte[] Value)>
        {
            (PreLoginOptionTokens.Version, version.PreLoginBytes()),
            (PreLoginOptionTokens.Encryption, [encryption]),
            (PreLoginOptionTokens.Instance, [0x00]),
            (PreLoginOptionTokens.ThreadId, []),
            (PreLoginOptionTokens.Mars, [0x00]),
        };

        // Not required: this server signs callers in with a SQL login, never a federated token.
        if (answerFederatedAuthentication)
            options.Add((PreLoginOptionTokens.FederatedAuthenticationRequired, [0x00]));

        var writer = new TdsPayloadWriter(64);
        var dataOffset = (options.Count * 5) + 1;
        foreach (var (token, value) in options)
        {
            writer.WriteByte(token);
            writer.WriteUInt16BigEndian((ushort)dataOffset);
            writer.WriteUInt16BigEndian((ushort)value.Length);
            dataOffset += value.Length;
        }

        writer.WriteByte(PreLoginOptionTokens.Terminator);
        foreach (var (_, value) in options)
            writer.WriteBytes(value);

        return writer.ToArray();
    }
}
