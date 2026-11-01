using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Protocols.Tds.Logging;

/// <summary>
/// MessageLogging for the TDS wire protocol: framing, pre-login, login and the requests a client sends.
/// </summary>
/// <remarks>
/// A password never appears here at any level, and neither does a session id: a login packet is logged
/// by its user name, application and database only.
/// </remarks>
[MessageLoggingTypeCode("TDS")]
public static partial class TdsProtocolLog
{
    /// <summary>Logs that a complete message was read from the client.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="packetType">The message's packet type.</param>
    /// <param name="length">The message's payload length in bytes.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Read a TDS message of type 0x{packetType:X2}, {length} bytes")]
    public static partial IGenericMessage MessageRead(ILogger logger, byte packetType, int length);

    /// <summary>Logs that a complete message was written to the client.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="packetType">The message's packet type.</param>
    /// <param name="length">The message's payload length in bytes.</param>
    /// <param name="packets">How many packets carried it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "Wrote a TDS message of type 0x{packetType:X2}, {length} bytes in {packets} packet(s)")]
    public static partial IGenericMessage MessageWritten(ILogger logger, byte packetType, int length, int packets);

    /// <summary>Logs the outcome of pre-login encryption negotiation.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="clientEncryption">What the client asked for.</param>
    /// <param name="serverEncryption">What this server answered.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Pre-login: the client offered encryption 0x{clientEncryption:X2}, this server answered 0x{serverEncryption:X2}")]
    public static partial IGenericMessage PreLoginNegotiated(ILogger logger, byte clientEncryption, byte serverEncryption);

    /// <summary>Logs that a login packet was read.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="tdsVersion">The TDS version the client asked for.</param>
    /// <param name="userName">The login name presented.</param>
    /// <param name="applicationName">The client application's name.</param>
    /// <param name="database">The database the client asked for, empty when none.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Login packet: TDS 0x{tdsVersion:X8}, login '{userName}', application '{applicationName}', database '{database}'")]
    public static partial IGenericMessage LoginRead(ILogger logger, uint tdsVersion, string userName, string applicationName, string database);

    /// <summary>Logs that the TLS handshake with the client completed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="protocol">The negotiated TLS protocol.</param>
    /// <param name="strict">True when the client opened with TLS (TDS 8 strict); false when TLS ran inside pre-login.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug,
        Message = "TLS established with the client: {protocol}, strict = {strict}")]
    public static partial IGenericMessage TlsEstablished(ILogger logger, string protocol, bool strict);

    /// <summary>Logs that something the client sent does not follow the protocol.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="what">What was being read.</param>
    /// <param name="reason">What was wrong with it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning,
        Message = "The client sent a malformed {what}: {reason}")]
    public static partial IGenericMessage Malformed(ILogger logger, string what, string reason);

    /// <summary>Logs that a message from the client is larger than this server accepts.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="length">The length the message had reached.</param>
    /// <param name="limit">The largest message this server accepts.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20003, Level = LogLevel.Warning,
        Message = "A message from the client reached {length} bytes, over the {limit}-byte limit")]
    public static partial IGenericMessage MessageTooLarge(ILogger logger, long length, int limit);

    /// <summary>Logs that the connection to the client failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="ex">The exception the transport raised.</param>
    /// <param name="operation">What this server was doing.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 70000, Level = LogLevel.Warning,
        Message = "The connection to the client failed while {operation}")]
    public static partial IGenericMessage TransportFailed(ILogger logger, Exception ex, string operation);

    /// <summary>Logs that a value cannot be written as the SQL type its column declares.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="column">The column being written.</param>
    /// <param name="valueType">The .NET type of the value.</param>
    /// <param name="sqlType">The SQL type the column declares.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 90002, Level = LogLevel.Error,
        Message = "Column '{column}' holds a {valueType} that cannot be written as SQL {sqlType}")]
    public static partial IGenericMessage ValueNotConvertible(ILogger logger, string column, string valueType, string sqlType);

    /// <summary>Logs that the client asked for something this server does not support.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="feature">What was asked for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 90004, Level = LogLevel.Warning,
        Message = "The client asked for {feature}, which this server does not support")]
    public static partial IGenericMessage NotSupported(ILogger logger, string feature);
}
