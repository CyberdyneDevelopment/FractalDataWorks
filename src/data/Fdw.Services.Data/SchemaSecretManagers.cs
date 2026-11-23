using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Builds the secret managers a host's <c>configurationSchema.json</c> declares and checks that every connection the schema declares can read the
/// secret it needs from them, before anything opens a store (FDW-6360).
/// </summary>
/// <remarks>
/// Every refusal is coded and names the manager, the connection, the key or the variable; none names a value. Nothing is defaulted: a manager of a kind nobody
/// registered, or one that cannot be built without the configuration store, stops the host, and so does a connection that names a manager the schema does not declare.
/// </remarks>
public static class SchemaSecretManagers
{
    /// <summary>Builds the provider of the declared managers.</summary>
    /// <param name="schema">The schema as loaded.</param>
    /// <param name="services">The container as it stands at registration, from which a kind's factory is taken.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The provider, which is empty when the schema declares none, or the coded failure that stopped the build.</returns>
    public static IGenericResult<SchemaSecretManagerProvider> Build(ConfigurationSchema schema, IServiceProvider services, ILogger logger)
    {
        var built = new Dictionary<string, ISecretManager>(StringComparer.OrdinalIgnoreCase);
        foreach (var declared in schema.SecretManagers)
        {
            var manager = BuildOne(declared, services, logger, built);
            if (manager.IsFailure || manager.Value is null)
                return manager.ToNewResult<SchemaSecretManagerProvider>();

            built[declared.Name] = manager.Value;
        }

        return GenericResult<SchemaSecretManagerProvider>.Success(new SchemaSecretManagerProvider(built));
    }

    /// <summary>Checks that every connection the schema declares that needs a secret names a key and a declared manager, and that the manager can read it.</summary>
    /// <param name="schema">The schema as loaded.</param>
    /// <param name="secretManagers">The provider of the declared managers.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the coded failure of the first connection that cannot get its secret.</returns>
    public static IGenericResult CheckConnections(
        ConfigurationSchema schema, ISecretManagerProvider secretManagers, ILogger logger, System.Threading.CancellationToken cancellationToken = default)
    {
        foreach (var connection in schema.Connections)
        {
            if (string.IsNullOrWhiteSpace(connection.Implementation)
                || ReferenceEquals(ConnectionTypes.ByName(connection.Implementation), ConnectionTypes.NotFound))
            {
                // The connection's own kind is refused where the gateway for it is built, with its own coded message.
                continue;
            }

            // The connection's own kind says whether it reads a stored secret: one that names no authentication method reads none, so it
            // is not asked for credential slots here (FDW-6387). CredentialSlots still refuses it where connections are saved and read.
            var connectionType = ConnectionTypes.ByName(connection.Implementation);
            var reads = connectionType.ReadsStoredSecret(connection);
            if (reads.IsFailure)
                return reads;

            if (!reads.Value)
                continue;

            var slots = connectionType.CredentialSlots(connection);
            if (slots.IsFailure || slots.Value is null)
                return slots;

            foreach (var credential in slots.Value.Credentials.Where(credential => credential.Secret))
            {
                var checkedSecret = CheckOne(connection.Name, credential.Field, slots.Value, secretManagers, logger, cancellationToken);
                if (checkedSecret.IsFailure)
                    return checkedSecret;
            }
        }

        return GenericResult.Success();
    }

    private static IGenericResult CheckOne(
        string connectionName,
        string field,
        Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots slots,
        ISecretManagerProvider secretManagers,
        ILogger logger,
        System.Threading.CancellationToken cancellationToken)
    {
        if (!slots.References.TryGetValue(field, out var key) || string.IsNullOrWhiteSpace(key))
            return GenericResult.Failure(ConfigurationGatewayProviderLog.ConnectionNamesNoSecretKey(logger, connectionName, field));

        if (slots.SecretManagerName is not { } managerName)
            return GenericResult.Failure(ConfigurationGatewayProviderLog.ConnectionNamesNoSecretManager(logger, connectionName, field));

        // Reading at start-up is what makes a variable that is not set a refusal of the host and not of its first query. The value is
        // read to prove it is there and dropped; nothing here keeps it, logs it or puts it in a message.
        // The Register phase is synchronous, and this read is the start-up proof; the same one-line form as ConfigurationGateway's boot-time build.
#pragma warning disable VSTHRD002
        var manager = secretManagers.Get(managerName, cancellationToken).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (manager.IsFailure || manager.Value is null)
            return GenericResult.Failure(ConfigurationGatewayProviderLog.ConnectionNamesUndeclaredSecretManager(logger, connectionName, managerName));

        // The Register phase is synchronous, and this read is the start-up proof; the same one-line form as ConfigurationGateway's boot-time build.
#pragma warning disable VSTHRD002
        var secret = manager.Value.Execute(GetSecretManagerCommand.Latest(null, key), cancellationToken).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (secret.IsFailure || secret.Value is null)
        {
            return GenericResult.Failure(ConfigurationGatewayProviderLog.ConnectionSecretNotAvailable(
                logger, connectionName, managerName, key, secret.CurrentMessage ?? string.Empty));
        }

        // A variable that is set to nothing is not a password: it is refused like one that is not set, and the reason says so without the value.
        return string.IsNullOrEmpty(secret.Value.GetStringValue())
            ? GenericResult.Failure(ConfigurationGatewayProviderLog.ConnectionSecretNotAvailable(
                logger, connectionName, managerName, key, "the secret's value is empty"))
            : GenericResult.Success();
    }

    private static IGenericResult<ISecretManager> BuildOne(
        ISecretManagerImplementationConfiguration declared, IServiceProvider services, ILogger logger, Dictionary<string, ISecretManager> built)
    {
        if (built.ContainsKey(declared.Name))
            return GenericResult<ISecretManager>.Failure(ConfigurationGatewayProviderLog.SecretManagerDeclaredTwice(logger, declared.Name));

        if (string.IsNullOrWhiteSpace(declared.Implementation))
            return GenericResult<ISecretManager>.Failure(ConfigurationGatewayProviderLog.SecretManagerDeclaresNoKind(logger, declared.Name));

        var kind = SecretManagerTypes.ByName(declared.Implementation);
        if (ReferenceEquals(kind, SecretManagerTypes.NotFound))
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerKindNotRegistered(logger, declared.Name, declared.Implementation));

        if (!kind.BuildableFromDeclarationAlone)
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerNeedsConfigurationStore(logger, declared.Name, declared.Implementation));

        if (services.GetService(kind.FactoryType) is not IServiceFactory<ISecretManager> factory)
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerFactoryUnavailable(logger, declared.Name, kind.FactoryType.Name));

        var created = factory.Create(declared);
        return created.IsSuccess && created.Value is not null
            ? created
            : created.IsFailure
                ? created
                : GenericResult<ISecretManager>.Failure(ConfigurationGatewayProviderLog.SecretManagerDeclaresNoBody(logger, declared.Name));
    }
}
