using System;

namespace Fdw.Protocols.Tds;

/// <summary>
/// One complete message from the client: the payload of every packet up to end-of-message.
/// </summary>
public sealed class TdsMessage
{
    /// <summary>
    /// The message that stands for a client that closed the connection cleanly between messages.
    /// </summary>
    /// <remarks>
    /// A clean close is not an error, and a reader that has nothing to answer still answers something:
    /// this sentinel, compared by reference, as a TypeCollection's NotFound is.
    /// </remarks>
    public static readonly TdsMessage Closed = new(0x00, [], resetsConnection: false);

    /// <summary>Initializes a new instance of the <see cref="TdsMessage"/> class.</summary>
    /// <param name="packetType">The packet type every packet of the message carried.</param>
    /// <param name="payload">The concatenated packet payloads.</param>
    /// <param name="resetsConnection">Whether the client asked for the session to be reset first.</param>
    public TdsMessage(byte packetType, byte[] payload, bool resetsConnection)
    {
        PacketType = packetType;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        ResetsConnection = resetsConnection;
    }

    /// <summary>Gets the packet type every packet of the message carried.</summary>
    public byte PacketType { get; }

    /// <summary>Gets the concatenated packet payloads.</summary>
    public byte[] Payload { get; }

    /// <summary>Gets a value indicating whether the client asked for the session to be reset first.</summary>
    public bool ResetsConnection { get; }
}
