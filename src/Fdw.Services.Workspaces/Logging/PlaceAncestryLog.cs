using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for reading the places above a place (FDW-2538).
/// EventId ranges: 11650 (done), 21650-21652 (a kind of place that is not known, cannot hold a version, or has no scope name, 400), 31650-31654 (a place that does not exist or is not the caller's, 404), 91650 (the tree is broken, 500).
/// </summary>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class PlaceAncestryLog
{
    /// <summary>Logs that the places at or above a place were read.</summary>
    [MessageLogging(EventId = 11650, Level = LogLevel.Debug, Message = "The places at or above {place} were read: {count} places")]
    public static partial IGenericMessage Read(ILogger logger, string place, int count);

    /// <summary>Logs a kind of place that is not known.</summary>
    [MessageLogging(EventId = 21650, Level = LogLevel.Warning, Message = "'{kind}' is not a kind of place: the places are the tenant, the organization and the workspace")]
    public static partial IGenericMessage KindUnknown(ILogger logger, string kind);

    /// <summary>Logs a tenant that does not exist.</summary>
    [MessageLogging(EventId = 31650, Level = LogLevel.Warning, Message = "There is no tenant '{id}'")]
    public static partial IGenericMessage TenantNotFound(ILogger logger, string id);

    /// <summary>Logs an organization that does not exist or was deleted.</summary>
    [MessageLogging(EventId = 31651, Level = LogLevel.Warning, Message = "There is no organization '{id}'")]
    public static partial IGenericMessage OrganizationNotFound(ILogger logger, string id);

    /// <summary>Logs a workspace that does not exist or was deleted.</summary>
    [MessageLogging(EventId = 31652, Level = LogLevel.Warning, Message = "There is no workspace '{id}'")]
    public static partial IGenericMessage WorkspaceNotFound(ILogger logger, string id);

    /// <summary>Logs a version asked for at a kind of place that cannot hold one.</summary>
    [MessageLogging(EventId = 21651, Level = LogLevel.Warning, Message = "A version cannot be held at a place of kind '{kind}': it is held at an organization or a workspace, and the tenant's version is the definition itself")]
    public static partial IGenericMessage NotVersionable(ILogger logger, string kind);

    /// <summary>Logs a name asked of a place that has none to show: the tenant, or a kind that is not known.</summary>
    [MessageLogging(EventId = 21652, Level = LogLevel.Warning, Message = "A place of kind '{kind}' has no scope name: only an organization and a workspace are named beside a definition")]
    public static partial IGenericMessage NoScopeName(ILogger logger, string kind);

    /// <summary>Logs a place that is not in the tenant the caller acts in.</summary>
    [MessageLogging(EventId = 31653, Level = LogLevel.Warning, Message = "{place} is not a place of the caller's tenant")]
    public static partial IGenericMessage NotInTenant(ILogger logger, string place);

    /// <summary>Logs a caller who acts in no tenant, so has no place to read or write definitions at.</summary>
    [MessageLogging(EventId = 31654, Level = LogLevel.Warning, Message = "The caller acts in no tenant, so there is no place to read or write definitions at")]
    public static partial IGenericMessage CallerHasNoPlace(ILogger logger);

    /// <summary>Logs a workspace that names neither an organization nor a tenant.</summary>
    [MessageLogging(EventId = 91650, Level = LogLevel.Error, Message = "Workspace '{id}' names neither an organization nor a tenant, so it sits nowhere in the scope tree")]
    public static partial IGenericMessage WorkspaceSitsNowhere(ILogger logger, string id);

    /// <summary>Logs a walk up the tree that passed every kind of place without reaching the top.</summary>
    [MessageLogging(EventId = 91651, Level = LogLevel.Error, Message = "The places above {place} climbed through every kind of place without reaching a tenant")]
    public static partial IGenericMessage TreeBroken(ILogger logger, string place);
}
