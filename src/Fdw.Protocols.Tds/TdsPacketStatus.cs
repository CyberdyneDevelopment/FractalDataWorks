namespace Fdw.Protocols.Tds;

/// <summary>
/// The status bits of the TDS packet header (MS-TDS 2.2.3.1.2).
/// </summary>
public static class TdsPacketStatus
{
    /// <summary>No status bit set: more packets of this message follow.</summary>
    public const byte Normal = 0x00;

    /// <summary>The last packet of the message.</summary>
    public const byte EndOfMessage = 0x01;

    /// <summary>The client asks this message to be ignored.</summary>
    public const byte Ignore = 0x02;

    /// <summary>The client asks the session to be reset before the request runs.</summary>
    public const byte ResetConnection = 0x08;

    /// <summary>As <see cref="ResetConnection"/>, keeping the transaction state.</summary>
    public const byte ResetConnectionSkipTransaction = 0x10;
}
