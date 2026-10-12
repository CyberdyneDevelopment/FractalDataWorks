namespace Fdw.Protocols.Tds;

/// <summary>
/// The values of the pre-login ENCRYPTION option (MS-TDS 2.2.6.5).
/// </summary>
public static class TdsEncryptionModes
{
    /// <summary>Encryption is available but off: only the login packet would be encrypted.</summary>
    public const byte Off = 0x00;

    /// <summary>Encryption is available and on.</summary>
    public const byte On = 0x01;

    /// <summary>Encryption is not available.</summary>
    public const byte NotSupported = 0x02;

    /// <summary>Encryption is required.</summary>
    public const byte Required = 0x03;

    /// <summary>
    /// Answers what this server replies to a client's offer. This server always encrypts the whole
    /// connection, so it answers <see cref="On"/> to a client that offers it and <see cref="Required"/>
    /// to one that prefers it off.
    /// </summary>
    /// <param name="client">The client's ENCRYPTION value.</param>
    /// <returns>The value to answer, or <see cref="NotSupported"/> when the client cannot encrypt and must be refused.</returns>
    public static byte Answer(byte client)
    {
        // The high bit asks for a client certificate, which this server does not use.
        var mode = (byte)(client & 0x0F);
        if (mode == NotSupported)
            return NotSupported;

        return mode == Off ? Required : On;
    }
}
