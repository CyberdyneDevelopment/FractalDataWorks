using System;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Decides whether a caller may save a connection, by whether the connection presents the host's own identity (FDW-1068).
/// </summary>
/// <remarks>
/// A connection that acts as the host -- Windows or Kerberos sign-in, a managed identity, the host's file system --
/// reaches whatever the host's identity reaches, including the platform's own databases and files. So creating or
/// changing one is a platform administrator's decision, and a connection that carries its own credential is anyone's
/// with the connection permission. The connection's type answers the question
/// (<see cref="IConnectionType.UsesHostIdentity"/>); a type that cannot answer is a refusal, never a "no".
/// </remarks>
public static class HostIdentityGate
{
    /// <summary>Admits or refuses saving <paramref name="connection"/>.</summary>
    /// <param name="connectionType">The connection's type, which answers whether it acts as the host.</param>
    /// <param name="connection">The connection configuration being saved.</param>
    /// <param name="callerIsPlatformAdmin">Whether the caller is a platform administrator.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// Success; <c>HostIdentityReservedToPlatform</c> when the connection acts as the host and the caller is not a
    /// platform administrator; or the type's <c>HostIdentityUndetermined</c> failure when it cannot answer.
    /// </returns>
    public static IGenericResult Admit(
        IConnectionType connectionType,
        IConnectionImplementationConfiguration connection,
        bool callerIsPlatformAdmin,
        ILogger logger)
    {
        if (connectionType is null) throw new ArgumentNullException(nameof(connectionType));
        if (connection is null) throw new ArgumentNullException(nameof(connection));

        var actsAsHost = connectionType.UsesHostIdentity(connection);
        if (actsAsHost.IsFailure)
            return actsAsHost;

        if (!actsAsHost.Value || callerIsPlatformAdmin)
            return GenericResult.Success();

        return GenericResult.Failure(
            ConnectionsResultCodes.ByName("HostIdentityReservedToPlatform"), logger,
            ResultDetails.Create("name", connection.Name, "connectionType", connectionType.Name));
    }
}
