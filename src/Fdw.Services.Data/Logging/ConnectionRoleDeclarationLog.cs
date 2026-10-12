using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for <c>ConnectionRoleDeclaration</c>: the host's declaration of which connection plays the platform,
/// server and auth role.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class ConnectionRoleDeclarationLog
{
    /// <summary>The host's configurationSchema.json carries no ConnectionRoles section at all.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostName">The host that is starting.</param>
    [MessageLogging(EventId = 61041, Level = LogLevel.Error,
        Message = "Host '{hostName}' declares no ConnectionRoles in configurationSchema.json; declare Platform and Server (and Auth when the host authenticates), each the name of a connection the same file declares")]
    public static partial IGenericMessage RolesNotDeclared(ILogger logger, string hostName);

    /// <summary>A required role is missing from the host's ConnectionRoles section.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostName">The host that is starting.</param>
    /// <param name="role">The role the host left out.</param>
    [MessageLogging(EventId = 61042, Level = LogLevel.Error,
        Message = "Host '{hostName}' declares no ConnectionRoles.{role} in configurationSchema.json, so the connection that plays that role is unknown; declare the name of one of the file's connections")]
    public static partial IGenericMessage RoleNotDeclared(ILogger logger, string hostName, string role);

    /// <summary>A role names a connection the host's configurationSchema.json does not declare.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostName">The host that is starting.</param>
    /// <param name="role">The role being declared.</param>
    /// <param name="connectionName">The name the host gave it.</param>
    [MessageLogging(EventId = 61043, Level = LogLevel.Error,
        Message = "Host '{hostName}' declares ConnectionRoles.{role} as '{connectionName}', but configurationSchema.json declares no connection by that name")]
    public static partial IGenericMessage RoleNamesUndeclaredConnection(ILogger logger, string hostName, string role, string connectionName);

    /// <summary>A role was already declared as a different connection in this process.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostName">The host that is starting.</param>
    /// <param name="role">The role being declared.</param>
    /// <param name="connectionName">The name this host gave it.</param>
    [MessageLogging(EventId = 61044, Level = LogLevel.Error,
        Message = "Host '{hostName}' declares ConnectionRoles.{role} as '{connectionName}', but this process already declared that role as a different connection; one process has one declaration")]
    public static partial IGenericMessage RoleAlreadyDeclaredDifferently(ILogger logger, string hostName, string role, string connectionName);

    /// <summary>The host's roles were read and recorded.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostName">The host that is starting.</param>
    /// <param name="platform">The platform connection.</param>
    /// <param name="server">The server connection.</param>
    [MessageLogging(EventId = 11572, Level = LogLevel.Information,
        Message = "Host '{hostName}' declared its connection roles: Platform='{platform}', Server='{server}'")]
    public static partial IGenericMessage RolesDeclared(ILogger logger, string hostName, string platform, string server);
}
