namespace Fdw.Protocols.Tds;

/// <summary>
/// The option tokens of a pre-login message (MS-TDS 2.2.6.5).
/// </summary>
public static class PreLoginOptionTokens
{
    /// <summary>The sender's version.</summary>
    public const byte Version = 0x00;

    /// <summary>The encryption the sender offers or requires.</summary>
    public const byte Encryption = 0x01;

    /// <summary>The instance name the client asks for.</summary>
    public const byte Instance = 0x02;

    /// <summary>The client's thread id.</summary>
    public const byte ThreadId = 0x03;

    /// <summary>Whether multiple active result sets are asked for.</summary>
    public const byte Mars = 0x04;

    /// <summary>The client's trace id.</summary>
    public const byte TraceId = 0x05;

    /// <summary>Whether federated authentication is required.</summary>
    public const byte FederatedAuthenticationRequired = 0x06;

    /// <summary>A nonce for federated authentication.</summary>
    public const byte Nonce = 0x07;

    /// <summary>The end of the option list.</summary>
    public const byte Terminator = 0xFF;
}
