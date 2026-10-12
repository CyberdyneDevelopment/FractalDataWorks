using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Reads the host's <c>ConnectionRoles</c> declaration and records it in <see cref="PlatformConnection"/>,
/// <see cref="ServerConnection"/> and <see cref="AuthConnection"/>.
/// </summary>
/// <remarks>
/// The one writer of those holders. A host that leaves a required role out, or names a connection its own schema does not
/// declare, does not start: the failure names the host and the declaration that is missing. Nothing here supplies a name
/// the host did not.
/// </remarks>
public static class ConnectionRoleDeclaration
{
    /// <summary>Records the roles <paramref name="schema"/> declares.</summary>
    /// <param name="schema">The host's bound <c>configurationSchema.json</c>.</param>
    /// <param name="hostName">The host that is starting, named in every failure.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A success result once the platform and server roles (and the auth role, when declared) are recorded; otherwise the first failure.</returns>
    public static IGenericResult Declare(ConfigurationSchema schema, string hostName, ILogger logger)
    {
        var roles = schema.ConnectionRoles;
        if (roles is null)
            return GenericResult.Failure(ConnectionRoleDeclarationLog.RolesNotDeclared(logger, hostName));

        var platform = DeclareRole(schema, hostName, "Platform", roles.Platform, PlatformConnection.Declare, true, logger);
        if (platform.IsFailure)
            return platform;

        var server = DeclareRole(schema, hostName, "Server", roles.Server, ServerConnection.Declare, true, logger);
        if (server.IsFailure)
            return server;

        var auth = DeclareRole(schema, hostName, "Auth", roles.Auth, AuthConnection.Declare, false, logger);
        if (auth.IsFailure)
            return auth;

        ConnectionRoleDeclarationLog.RolesDeclared(logger, hostName, PlatformConnection.Name, ServerConnection.Name);
        return GenericResult.Success();
    }

    private static IGenericResult DeclareRole(
        ConfigurationSchema schema,
        string hostName,
        string role,
        string? connectionName,
        Func<string, bool> record,
        bool required,
        ILogger logger)
    {
        if (connectionName is null || string.IsNullOrWhiteSpace(connectionName))
            return required
                ? GenericResult.Failure(ConnectionRoleDeclarationLog.RoleNotDeclared(logger, hostName, role))
                : GenericResult.Success();

        if (!schema.Connections.Any(connection => string.Equals(connection.Name, connectionName, StringComparison.OrdinalIgnoreCase)))
            return GenericResult.Failure(ConnectionRoleDeclarationLog.RoleNamesUndeclaredConnection(logger, hostName, role, connectionName));

        return record(connectionName)
            ? GenericResult.Success()
            : GenericResult.Failure(ConnectionRoleDeclarationLog.RoleAlreadyDeclaredDifferently(logger, hostName, role, connectionName));
    }
}
