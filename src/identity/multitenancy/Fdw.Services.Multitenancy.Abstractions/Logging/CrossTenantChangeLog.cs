using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Abstractions.Logging;

/// <summary>Messages of the decision whether an administrator of one tenant may change another tenant's object (FDW-4459).</summary>
[MessageLoggingTypeCode("CROSSTENANT")]
public static partial class CrossTenantChangeLog
{
    /// <summary>Logs that a change was allowed.</summary>
    [MessageLogging(EventId = 13300, Level = LogLevel.Debug, Message = "A change under '{changeName}' of tenant {ownerTenantId}'s object by tenant {actorTenantId} is allowed ({reason})")]
    public static partial IGenericMessage Allowed(ILogger logger, string changeName, Guid ownerTenantId, Guid actorTenantId, string reason);

    /// <summary>Logs that a change was refused because the setting is off. The number is in the Forbidden category (10).</summary>
    [MessageLogging(EventId = 103200, Level = LogLevel.Warning, Message = "A change of tenant {ownerTenantId}'s object by an administrator of tenant {actorTenantId} is refused: the setting '{changeName}' is off",
        ClientMessage = "This belongs to another tenant, and the setting '{changeName}' does not allow an administrator of a different tenant to change it")]
    public static partial IGenericMessage Refused(ILogger logger, string changeName, Guid ownerTenantId, Guid actorTenantId);

    /// <summary>Logs that a change was refused because the caller has no active tenant. The number is in the Forbidden category (10).</summary>
    [MessageLogging(EventId = 103201, Level = LogLevel.Warning, Message = "A change of tenant {ownerTenantId}'s object under '{changeName}' is refused: the caller has no active tenant to compare it with",
        ClientMessage = "The change under '{changeName}' was refused because you have no active tenant to compare it with")]
    public static partial IGenericMessage ActorTenantMissing(ILogger logger, string changeName, Guid ownerTenantId);

    /// <summary>Logs that a setting is declared by neither a settings row, the host nor the option.</summary>
    [MessageLogging(EventId = 60001, Level = LogLevel.Error, Message = "The setting '{changeName}' is stated by no settings row and declared by neither the option nor the host, so a change of another tenant's object was not allowed", ClientMessage = "The setting '{changeName}' is stated by no settings row and declared by neither the option nor the host, so a change of another tenant's object was not allowed")]
    public static partial IGenericMessage SettingNotDeclared(ILogger logger, string changeName);
}
