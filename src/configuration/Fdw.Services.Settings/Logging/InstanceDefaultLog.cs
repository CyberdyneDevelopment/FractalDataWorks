using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Logging;

/// <summary>
/// MessageLogging for a value that is not stated and whose default, a setting, is not there (FDW-3580).
/// EventId range: 62050-62059.
/// </summary>
[MessageLoggingTypeCode("SETTINGS")]
public static partial class InstanceDefaultLog
{
    // Why Error: what is being made is not made, so nothing that needs it can use it.
    /// <summary>Logs a value that is not stated when the setting that gives its default is not set.</summary>
    [MessageLogging(EventId = 62050, Level = LogLevel.Error,
        Message = "The {ownerKind} '{ownerName}' states no {field} and the setting '{setting}' that gives its default is not set",
        ClientMessage = "The {ownerKind} '{ownerName}' states no {field}, and the setting '{setting}' that gives the default is not set")]
    public static partial IGenericMessage NotStated(ILogger logger, string ownerKind, string ownerName, string field, string setting);

    /// <summary>Logs a value that is not stated in a host that has no settings to read its default from.</summary>
    [MessageLogging(EventId = 62051, Level = LogLevel.Error,
        Message = "The {ownerKind} '{ownerName}' states no {field} and this host registers no settings to read '{setting}' from",
        ClientMessage = "The {ownerKind} '{ownerName}' states no {field}, and this host has no settings to read '{setting}' from")]
    public static partial IGenericMessage NoSettings(ILogger logger, string ownerKind, string ownerName, string field, string setting);

    /// <summary>Logs a value that is not stated when the settings that would give it are read through the owner being made.</summary>
    [MessageLogging(EventId = 62052, Level = LogLevel.Error,
        Message = "The {ownerKind} '{ownerName}' states no {field}, and the setting '{setting}' cannot be read yet because the settings are read through the {ownerKind} being made; state {field} in its configuration",
        ClientMessage = "The {ownerKind} '{ownerName}' states no {field}, and the settings that would give one are read through it; state {field} in its configuration")]
    public static partial IGenericMessage ReadThroughItself(ILogger logger, string ownerKind, string ownerName, string field, string setting);

    /// <summary>Logs a create body that leaves a value out and takes it from the setting (FDW-3823).</summary>
    [MessageLogging(EventId = 62053, Level = LogLevel.Trace,
        Message = "The {ownerKind} '{ownerName}' takes {field} from the setting '{setting}'")]
    public static partial IGenericMessage Taken(ILogger logger, string ownerKind, string ownerName, string field, string setting);

    // Why Error: a defect of the configuration type, not of the request; the property must be text, a number or a flag.
    /// <summary>Logs a configuration property that names a default setting but is of a type no setting is read as (FDW-3823).</summary>
    [MessageLogging(EventId = 62054, Level = LogLevel.Error,
        Message = "The {ownerKind} '{ownerName}' cannot take {field} from the setting '{setting}': a {type} is not read from a setting")]
    public static partial IGenericMessage TypeUnsupported(ILogger logger, string ownerKind, string ownerName, string field, string setting, string type);

    /// <summary>Logs a create body that leaves a value out and takes the tuning default its service type carries (FDW-3823).</summary>
    [MessageLogging(EventId = 62055, Level = LogLevel.Trace,
        Message = "The {ownerKind} '{ownerName}' takes {field} from its type {typeName}")]
    public static partial IGenericMessage TakenFromType(ILogger logger, string ownerKind, string ownerName, string field, string typeName);

    // Why Warning: the request is refused; the caller must state the value, nothing is stored.
    /// <summary>Logs a create body that leaves out a value only the caller can choose, which has no default (FDW-3823).</summary>
    [MessageLogging(EventId = 22056, Level = LogLevel.Warning,
        Message = "The {ownerKind} '{ownerName}' states no {field}, which is the caller's choice and has no default",
        ClientMessage = "The {ownerKind} '{ownerName}' states no {field}; it is required")]
    public static partial IGenericMessage ChoiceNotStated(ILogger logger, string ownerKind, string ownerName, string field);

    /// <summary>Logs a create body that leaves out a value resolved where the service is built, which is not written into the row (FDW-4904).</summary>
    [MessageLogging(EventId = 62057, Level = LogLevel.Trace,
        Message = "The {ownerKind} '{ownerName}' states no {field}; it is left empty and takes its type's default when it is built")]
    public static partial IGenericMessage LeftForUse(ILogger logger, string ownerKind, string ownerName, string field);
}
