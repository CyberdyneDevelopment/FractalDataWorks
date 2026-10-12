namespace Fdw.Protocols.Tds;

/// <summary>
/// The packet types of the TDS packet header (MS-TDS 2.2.3.1.1).
/// </summary>
/// <remarks>
/// Wire values fixed by the protocol, not a set this server chooses: each names the kind of message a
/// packet carries, and a message's packets all carry the same type.
/// </remarks>
public static class TdsPacketTypes
{
    /// <summary>A SQL batch: Unicode T-SQL text.</summary>
    public const byte SqlBatch = 0x01;

    /// <summary>A remote procedure call.</summary>
    public const byte Rpc = 0x03;

    /// <summary>A tabular result: every response this server sends.</summary>
    public const byte TabularResult = 0x04;

    /// <summary>An attention signal: the client cancels the request in progress.</summary>
    public const byte Attention = 0x06;

    /// <summary>Bulk load data.</summary>
    public const byte BulkLoad = 0x07;

    /// <summary>A federated authentication token.</summary>
    public const byte FederatedAuthenticationToken = 0x08;

    /// <summary>A transaction manager request.</summary>
    public const byte TransactionManager = 0x0E;

    /// <summary>A TDS 7 login.</summary>
    public const byte Login7 = 0x10;

    /// <summary>An SSPI (integrated authentication) message.</summary>
    public const byte Sspi = 0x11;

    /// <summary>A pre-login message, and the TLS handshake records TDS 7 carries inside them.</summary>
    public const byte PreLogin = 0x12;

    /// <summary>The first byte of a TLS handshake record: a TDS 8 client opens with TLS, not pre-login.</summary>
    public const byte TlsHandshakeRecord = 0x16;
}
