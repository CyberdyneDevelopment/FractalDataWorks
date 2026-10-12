using System;
using Fdw.Protocols.Tds.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Protocols.Tds;

/// <summary>
/// What a client offered in its pre-login message (MS-TDS 2.2.6.5).
/// </summary>
public sealed class PreLoginRequest
{
    private PreLoginRequest(byte encryption, bool asksForMars, bool asksForFederatedAuthentication)
    {
        Encryption = encryption;
        AsksForMars = asksForMars;
        AsksForFederatedAuthentication = asksForFederatedAuthentication;
    }

    /// <summary>Gets the client's ENCRYPTION value.</summary>
    public byte Encryption { get; }

    /// <summary>Gets a value indicating whether the client asked for multiple active result sets.</summary>
    public bool AsksForMars { get; }

    /// <summary>Gets a value indicating whether the client sent the federated-authentication option.</summary>
    public bool AsksForFederatedAuthentication { get; }

    /// <summary>Parses a pre-login payload.</summary>
    /// <param name="payload">The message payload.</param>
    /// <param name="logger">The logger for a malformed message.</param>
    /// <returns>What the client offered, or the refusal.</returns>
    public static IGenericResult<PreLoginRequest> Parse(byte[] payload, ILogger logger)
    {
        var reader = new TdsPayloadReader(payload);
        var offered = new Offered();
        var more = true;
        while (more)
            more = ReadOption(reader, offered);

        if (reader.Overrun)
            return GenericResult<PreLoginRequest>.Failure(TdsProtocolLog.Malformed(logger, "pre-login message", "an option runs past the end of the payload"));

        if (offered.Encryption is not { } encryption)
            return GenericResult<PreLoginRequest>.Failure(TdsProtocolLog.Malformed(logger, "pre-login message", "it carries no ENCRYPTION option"));

        return GenericResult<PreLoginRequest>.Success(new PreLoginRequest(encryption, offered.Mars, offered.Federated));
    }

    // Reads one option into `offered`; false at the terminator or the end of the payload.
    private static bool ReadOption(TdsPayloadReader reader, Offered offered)
    {
        var token = reader.ReadByte();
        if (token == PreLoginOptionTokens.Terminator || reader.Overrun)
            return false;

        var offset = reader.ReadUInt16BigEndian();
        var length = reader.ReadUInt16BigEndian();
        var value = reader.BytesAt(offset, length);

        if (token == PreLoginOptionTokens.Encryption && value.Length >= 1)
            offered.Encryption = value[0];
        else if (token == PreLoginOptionTokens.Mars && value.Length >= 1)
            offered.Mars = value[0] != 0;
        else if (token == PreLoginOptionTokens.FederatedAuthenticationRequired)
            offered.Federated = true;

        return !reader.Overrun;
    }

    private sealed class Offered
    {
        public byte? Encryption { get; set; }

        public bool Mars { get; set; }

        public bool Federated { get; set; }
    }
}
