using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for how a connection type binds, merges and shows its configuration through the
/// connection endpoints (<see cref="ConnectionEndpointBehavior"/>).
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ConnectionEndpointBehaviorLog
{
    /// <summary>Logged when a body cannot be read into the connection type's configuration.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning,
        Message = "The configuration for a '{configurationType}' connection is invalid: {reason}")]
    public static partial IGenericMessage ConfigurationInvalid(ILogger logger, string configurationType, string reason);

    /// <summary>Logged when a configuration was read but one of the connection type's own rules refused it.</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning,
        Message = "The configuration for a '{configurationType}' connection failed validation: {details}",
        ClientMessage = "The configuration for a '{configurationType}' connection failed validation: {details}")]
    public static partial IGenericMessage ConfigurationRejected(ILogger logger, string configurationType, string details);

    /// <summary>Logged when a connection's authentication holds a key its authentication type does not expect (FDW-4607).</summary>
    [MessageLogging(EventId = 20003, Level = LogLevel.Warning,
        Message = "The '{authenticationType}' authentication of a '{configurationType}' connection holds keys it does not expect: {keys}",
        ClientMessage = "The '{authenticationType}' authentication of a '{configurationType}' connection holds keys it does not expect: {keys}. Send only the keys that authentication type declares.")]
    public static partial IGenericMessage AuthenticationPropertyNotExpected(ILogger logger, string configurationType, string authenticationType, string keys);

    /// <summary>Logged when a connection kind declares it has no authentication dictionary, yet its configuration holds one (FDW-4607).</summary>
    [MessageLogging(EventId = 20004, Level = LogLevel.Error,
        Message = "The '{configurationType}' connection type declares no authentication but its configuration holds the string dictionaries {dictionaries}",
        ClientMessage = "The '{configurationType}' connection type cannot save a connection: it declares no authentication but its configuration holds the string dictionaries {dictionaries}.")]
    public static partial IGenericMessage AuthenticationNotDeclared(ILogger logger, string configurationType, string dictionaries);

    /// <summary>Logged when a connection type's configuration class cannot be bound at all.</summary>
    [MessageLogging(EventId = 60003, Level = LogLevel.Error,
        Message = "Configuration type '{configurationType}' is not a concrete connection configuration, so a connection of this kind cannot be created or updated through the endpoints")]
    public static partial IGenericMessage ConfigurationTypeNotBindable(ILogger logger, string configurationType);

    /// <summary>Logged when a stored configuration cannot be shown as JSON.</summary>
    [MessageLogging(EventId = 90002, Level = LogLevel.Error,
        Message = "The '{configurationType}' configuration could not be shown as JSON: {reason}")]
    public static partial IGenericMessage ProjectionFailed(ILogger logger, string configurationType, string reason);
}
