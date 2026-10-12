using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Connections.Endpoints.Logging;
using Fdw.Services.Connections.Results;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Stores a connection's credentials for the connection endpoints: the values a caller sends are written to the
/// platform's credential secret manager under secrets the server chooses, and the connection is pointed at them.
/// </summary>
/// <remarks>
/// <para>
/// A credential belongs to one connection, and setting it comes with being allowed to edit that connection; there is
/// no separate permission, and no way to read a credential back (FDW-1086). The caller never names the secret manager
/// or the secret: the secret manager comes from the server setting
/// <see cref="SettingDefinitions.ConnectionCredentialSecretManager"/>, and each secret's name is minted here.
/// </para>
/// <para>
/// No value is logged or returned. Values are resolved server-side only when the connection is used.
/// </para>
/// </remarks>
public static class ConnectionCredentialStore
{
    // What every stored connection credential is called in the store's own secret-type column. The store requires a type
    // and assumes none (a secret manager that records one refuses a command without it), so the caller states it here.
    private const string StoredSecretType = "ConnectionCredential";

    // Shown when the credential store's failure carried neither a result code nor a coded message: the report says so
    // rather than inventing one.
    private const string StoreResultNotReported = "not reported";

    /// <summary>
    /// Readies a connection's credentials for a save: refuses a request that names where they are stored, then stores
    /// the values it sent (<see cref="Store"/>).
    /// </summary>
    /// <param name="connectionType">The connection's type.</param>
    /// <param name="before">The connection's credential slots before the request, or <see langword="null"/> for a new connection.</param>
    /// <param name="connection">The connection's configuration as the request leaves it; its credential references are changed in place.</param>
    /// <param name="credentials">The values the request sent, or <see langword="null"/> when it sent none.</param>
    /// <param name="services">The request's services.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What was stored and what it replaced, or the refusal.</returns>
    public static async Task<IGenericResult<ConnectionCredentialWrite>> Prepare(
        IConnectionType connectionType,
        ConnectionCredentialSlots? before,
        IConnectionImplementationConfiguration connection,
        IDictionary<string, string>? credentials,
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var references = RefuseCallerReferences(connectionType, before, connection, logger);
        if (references.IsFailure)
            return references.ToNewResult<ConnectionCredentialWrite>();

        var stored = await Store(connectionType, connection, credentials, services, logger, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
            return stored;

        // Secrets the connection named before but names no more (a method that carries fewer credentials, or none) are
        // retired with the ones the new values replace.
        return GenericResult<ConnectionCredentialWrite>.Success(stored.Value!.AlsoRetiring(Dropped(connectionType, before, connection)));
    }

    /// <summary>
    /// Refuses a change to where a connection's credentials are stored that came from the caller rather than the
    /// server: a credential field or the secret manager set to a value other than the one the connection already had.
    /// </summary>
    /// <param name="connectionType">The connection's type.</param>
    /// <param name="before">The connection's credential slots before the request, or <see langword="null"/> for a new connection.</param>
    /// <param name="after">The connection's configuration as the request would leave it.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <returns>Success, or a <c>CredentialReferenceReserved</c> refusal naming the value the caller tried to set.</returns>
    /// <remarks>
    /// Clearing a reference is allowed (switching to a method that carries no credential); naming one is not, because a
    /// caller who could name the secret could make the connection read any secret the platform holds.
    /// </remarks>
    public static IGenericResult RefuseCallerReferences(
        IConnectionType connectionType,
        ConnectionCredentialSlots? before,
        IConnectionImplementationConfiguration after,
        ILogger logger)
    {
        if (connectionType is null) throw new ArgumentNullException(nameof(connectionType));
        if (after is null) throw new ArgumentNullException(nameof(after));

        var slots = connectionType.CredentialSlots(after);
        if (slots.IsFailure)
            return slots;

        var chosen = ChosenByCaller(before, slots.Value!);
        if (chosen is null)
            return GenericResult.Success();

        ConnectionEndpointLog.CredentialReferenceRefused(logger, after.Name, chosen);
        return GenericResult.Failure(
            ConnectionsResultCodes.ByName("CredentialReferenceReserved"),
            ResultDetails.Create("name", after.Name, "property", chosen));
    }

    /// <summary>
    /// Stores the credential values a request sent and points the connection at them, or confirms the connection
    /// already has every credential its authentication method uses.
    /// </summary>
    /// <param name="connectionType">The connection's type.</param>
    /// <param name="connection">The connection's configuration; its credential references are changed in place.</param>
    /// <param name="credentials">The values the request sent, keyed by credential field, or <see langword="null"/> when it sent none.</param>
    /// <param name="services">The request's services, for the settings and the secret managers.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// What was stored and what it replaced, for <see cref="Retire"/> to clean up after the save; or a refusal saying
    /// which credentials are missing, which fields were wrong, or that no credential secret manager is configured.
    /// </returns>
    public static async Task<IGenericResult<ConnectionCredentialWrite>> Store(
        IConnectionType connectionType,
        IConnectionImplementationConfiguration connection,
        IDictionary<string, string>? credentials,
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (connectionType is null) throw new ArgumentNullException(nameof(connectionType));
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        if (services is null) throw new ArgumentNullException(nameof(services));

        var slots = connectionType.CredentialSlots(connection);
        if (slots.IsFailure)
            return slots.ToNewResult<ConnectionCredentialWrite>();

        var existing = slots.Value!;
        if (credentials is null || credentials.Count == 0)
            return RequireStored(connection, existing);

        var supplied = CheckFields(connection, existing, credentials);
        if (supplied.IsFailure)
            return supplied.ToNewResult<ConnectionCredentialWrite>();

        var manager = await CredentialManager(services, logger, cancellationToken).ConfigureAwait(false);
        if (manager.IsFailure)
            return manager.ToNewResult<ConnectionCredentialWrite>();

        var (managerName, secretManager) = manager.Value;
        var written = await Write(secretManager, supplied.Value!, logger, connection.Name, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written.ToNewResult<ConnectionCredentialWrite>();

        var keys = written.Value!;
        var bound = connectionType.BindCredentials(connection, managerName, keys);
        if (bound.IsFailure)
        {
            await Delete(secretManager, keys.Values, connection.Name, logger, cancellationToken).ConfigureAwait(false);
            return bound.ToNewResult<ConnectionCredentialWrite>();
        }

        ConnectionEndpointLog.CredentialsStored(logger, connection.Name, keys.Count);
        return GenericResult<ConnectionCredentialWrite>.Success(new ConnectionCredentialWrite(
            keys.Values.Select(key => new StoredSecret(managerName, key)).ToList(),
            Named(existing, keys.Keys)));
    }

    /// <summary>
    /// Deletes secrets a connection no longer names: the ones a save replaced, or, when the save failed, the ones just written.
    /// Only secrets the store minted are deleted (<see cref="ConnectionCredentialSecretName.IsMinted"/>); any other name is a
    /// secret someone else keeps, and it is logged and left (FDW-1206).
    /// </summary>
    /// <param name="secrets">The secrets to delete.</param>
    /// <param name="connectionName">The connection they belonged to, for the log.</param>
    /// <param name="services">The request's services, for the secret managers.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when every secret has been asked to be deleted.</returns>
    /// <remarks>
    /// A secret that cannot be deleted is logged and left: the connection no longer names it, so it is never read again,
    /// and failing the request after the connection was saved would report a save that happened as one that did not.
    /// </remarks>
    public static async Task Retire(
        IReadOnlyList<StoredSecret> secrets,
        string connectionName,
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (secrets is null) throw new ArgumentNullException(nameof(secrets));
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (secrets.Count == 0)
            return;

        var provider = services.GetRequiredService<ISecretManagerProvider>();
        foreach (var group in MintedOnly(secrets, connectionName, logger).GroupBy(secret => secret.SecretManagerName, StringComparer.Ordinal))
        {
            var manager = await provider.Get(group.Key, cancellationToken).ConfigureAwait(false);
            if (manager.IsFailure || manager.Value is null)
            {
                ConnectionEndpointLog.CredentialSecretManagerUnavailable(logger, connectionName, group.Key);
                continue;
            }

            await Delete(manager.Value, group.Select(secret => secret.SecretKey), connectionName, logger, cancellationToken).ConfigureAwait(false);
        }
    }

    // The secrets the store may delete: the ones it minted. A name the store did not mint is a secret someone else keeps and
    // the connection only points at; it is logged and left, here at the one place every secret delete passes through.
    private static List<StoredSecret> MintedOnly(IReadOnlyList<StoredSecret> secrets, string connectionName, ILogger logger)
    {
        var minted = new List<StoredSecret>(secrets.Count);
        foreach (var secret in secrets)
        {
            if (ConnectionCredentialSecretName.IsMinted(secret.SecretKey))
                minted.Add(secret);
            else
                ConnectionEndpointLog.CredentialLeftAlone(logger, connectionName, secret.SecretManagerName, secret.SecretKey);
        }

        return minted;
    }

    /// <summary>
    /// The secrets a connection's credential slots name, under the secret manager they name. Deleting them is
    /// <see cref="Retire"/>'s decision: it deletes only the ones the store minted.
    /// </summary>
    /// <param name="connectionType">The connection's type.</param>
    /// <param name="connection">The connection as stored.</param>
    /// <returns>The secrets, none when the connection names none; or the refusal when its credentials cannot be determined.</returns>
    public static IGenericResult<IReadOnlyList<StoredSecret>> Owned(IConnectionType connectionType, IConnectionImplementationConfiguration connection)
    {
        if (connectionType is null) throw new ArgumentNullException(nameof(connectionType));
        if (connection is null) throw new ArgumentNullException(nameof(connection));

        var slots = connectionType.CredentialSlots(connection);
        if (slots.IsFailure)
            return slots.ToNewResult<IReadOnlyList<StoredSecret>>();

        return GenericResult<IReadOnlyList<StoredSecret>>.Success(Named(slots.Value!, slots.Value!.Fields));
    }

    /// <summary>
    /// Deletes a connection together with the secrets it owns: the secrets are read first, the connection is deleted, and
    /// the secrets are deleted only once the connection is gone (FDW-1202).
    /// </summary>
    /// <param name="connectionType">The connection's type.</param>
    /// <param name="connection">The connection as stored, read before the delete.</param>
    /// <param name="deleteConnection">Deletes the connection itself.</param>
    /// <param name="services">The request's services, for the secret managers.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The connection delete's result; a connection whose credentials cannot be determined is refused and not deleted.</returns>
    /// <remarks>
    /// Why the secrets go second: a connection that failed to delete still names them, and deleting them first would leave
    /// a live connection pointing at nothing. A secret that cannot be deleted is logged by <see cref="Retire"/> and left;
    /// the connection is gone, so nothing reads it again, and failing a delete that happened would report it as one that did not.
    /// </remarks>
    public static async Task<IGenericResult> DeleteConnection(
        IConnectionType connectionType,
        IConnectionImplementationConfiguration connection,
        Func<CancellationToken, Task<IGenericResult>> deleteConnection,
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (deleteConnection is null) throw new ArgumentNullException(nameof(deleteConnection));

        var owned = Owned(connectionType, connection);
        if (owned.IsFailure)
            return owned;

        var deleted = await deleteConnection(cancellationToken).ConfigureAwait(false);
        if (deleted.IsSuccess)
            await Retire(owned.Value!, connection.Name, services, logger, cancellationToken).ConfigureAwait(false);

        return deleted;
    }

    // The first credential reference the caller set to a value of its own: a field or the secret manager that now names
    // something the connection did not name before. A cleared value is not a choice.
    private static string? ChosenByCaller(ConnectionCredentialSlots? before, ConnectionCredentialSlots after)
    {
        foreach (var field in after.Fields)
        {
            after.References.TryGetValue(field, out var now);
            string? was = null;
            before?.References.TryGetValue(field, out was);
            if (!string.IsNullOrEmpty(now) && !string.Equals(now, was, StringComparison.Ordinal))
                return field;
        }

        return after.SecretManagerName is { } manager
               && !string.Equals(manager, before?.SecretManagerName, StringComparison.Ordinal)
            ? ConnectionCredentialProperties.SecretManager
            : null;
    }

    // With no values sent, every credential the method uses must already be stored.
    private static IGenericResult<ConnectionCredentialWrite> RequireStored(
        IConnectionImplementationConfiguration connection, ConnectionCredentialSlots existing)
    {
        if (existing.MissingFields.Count == 0)
            return GenericResult<ConnectionCredentialWrite>.Success(ConnectionCredentialWrite.Nothing);

        return GenericResult<ConnectionCredentialWrite>.Failure(
            ConnectionsResultCodes.ByName("CredentialRequired"),
            ResultDetails.Create(
                "name", connection.Name,
                "method", existing.Method,
                "missing", string.Join(", ", existing.MissingFields)));
    }

    // The values sent must name exactly the credentials the method uses, each with a value. The field names are
    // returned as the method spells them.
    private static IGenericResult<IReadOnlyDictionary<string, string>> CheckFields(
        IConnectionImplementationConfiguration connection,
        ConnectionCredentialSlots existing,
        IDictionary<string, string> credentials)
    {
        var byField = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in existing.Fields)
        {
            var sent = credentials.FirstOrDefault(entry => string.Equals(entry.Key, field, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(sent.Value))
                byField[field] = sent.Value;
        }

        var unknown = credentials.Keys.Any(key => !existing.Fields.Contains(key, StringComparer.OrdinalIgnoreCase));
        if (!unknown && byField.Count == existing.Fields.Count && existing.Fields.Count > 0)
            return GenericResult<IReadOnlyDictionary<string, string>>.Success(byField);

        return GenericResult<IReadOnlyDictionary<string, string>>.Failure(
            ConnectionsResultCodes.ByName("CredentialFieldsInvalid"),
            ResultDetails.Create(
                "name", connection.Name,
                "method", existing.Method,
                "expected", string.Join(", ", existing.Fields))
                .With("supplied", string.Join(", ", credentials.Where(entry => !string.IsNullOrEmpty(entry.Value)).Select(entry => entry.Key))));
    }

    // The secret manager the server setting names. Nothing guesses one when the setting is empty.
    private static async Task<IGenericResult<(string Name, ISecretManager Manager)>> CredentialManager(
        IServiceProvider services, ILogger logger, CancellationToken cancellationToken)
    {
        // Both are platform services every host registers; a host that lacks one is a wiring defect, and resolving
        // them as required makes it fail loudly instead of reading as "not configured".
        var settings = services.GetRequiredService<IEffectiveSettingsProvider>();
        var secrets = services.GetRequiredService<ISecretManagerProvider>();

        var name = settings.GetEffectiveValue<string>(SettingDefinitions.ConnectionCredentialSecretManager);
        if (string.IsNullOrWhiteSpace(name))
        {
            ConnectionEndpointLog.CredentialStoreNotConfigured(logger, SettingDefinitions.ConnectionCredentialSecretManager);
            return GenericResult<(string, ISecretManager)>.Failure(
                ConnectionsResultCodes.ByName("CredentialStoreNotConfigured"),
                ResultDetails.Create("setting", SettingDefinitions.ConnectionCredentialSecretManager));
        }

        var manager = await secrets.Get(name, cancellationToken).ConfigureAwait(false);
        if (manager.IsFailure)
            return manager.ToNewResult<(string, ISecretManager)>();

        return manager.Value is { } opened
            ? GenericResult<(string, ISecretManager)>.Success((name, opened))
            : GenericResult<(string, ISecretManager)>.Failure(
                ConnectionsResultCodes.ByName("CredentialStoreNotConfigured"),
                ResultDetails.Create("setting", SettingDefinitions.ConnectionCredentialSecretManager));
    }

    // Writes each value under a fresh secret name and returns the name per field. A failure deletes what was written.
    private static async Task<IGenericResult<IReadOnlyDictionary<string, string>>> Write(
        ISecretManager secretManager,
        IReadOnlyDictionary<string, string> valueByField,
        ILogger logger,
        string connectionName,
        CancellationToken cancellationToken)
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var credential in valueByField)
        {
            var minted = ConnectionCredentialSecretName.Mint(connectionName, credential.Key);
            if (minted.IsFailure)
            {
                await Delete(secretManager, keys.Values, connectionName, logger, cancellationToken).ConfigureAwait(false);
                return minted.ToNewResult<IReadOnlyDictionary<string, string>>();
            }

            var key = minted.Value!;
            var set = await secretManager
                .Execute(
                    SetSecretManagerCommand.WithSecretType(container: null, secretKey: key, secretValue: credential.Value, secretType: StoredSecretType),
                    cancellationToken)
                .ConfigureAwait(false);
            if (set.IsFailure)
            {
                await Delete(secretManager, keys.Values, connectionName, logger, cancellationToken).ConfigureAwait(false);

                // The store's own failure is its business (a validation code of its own would read to the caller as a bad
                // request when the request was fine): the caller learns which credential could not be stored and the
                // store's result code; the store's message stays in the log.
                var reason = StoreResultCode(set);
                ConnectionEndpointLog.CredentialWriteFailed(logger, connectionName, credential.Key, reason);
                return GenericResult<IReadOnlyDictionary<string, string>>.Failure(
                    ConnectionsResultCodes.ByName("CredentialPersistFailed"),
                    ResultDetails.Create("name", connectionName, "field", credential.Key, "reason", reason));
            }

            keys[credential.Key] = key;
        }

        return GenericResult<IReadOnlyDictionary<string, string>>.Success(keys);
    }

    // The result code the credential store reported for a failed write: the code on the result when it returned one,
    // else the code of the message it logged.
    private static string StoreResultCode(IGenericResult failure)
    {
        var cause = failure.RootCause;
        if (cause.Code is { } code)
            return code.Code;

        var messageCode = cause.Messages.Select(message => message.Code).FirstOrDefault(candidate => !string.IsNullOrEmpty(candidate));
        return messageCode is null ? StoreResultNotReported : messageCode;
    }

    private static async Task Delete(
        ISecretManager secretManager,
        IEnumerable<string> secretKeys,
        string connectionName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var key in secretKeys)
        {
            var deleted = await secretManager
                .Execute(DeleteSecretManagerCommand.PermanentlyDelete(container: null, secretKey: key), cancellationToken)
                .ConfigureAwait(false);
            if (deleted.IsFailure)
                ConnectionEndpointLog.CredentialRetireFailed(logger, connectionName, key);
        }
    }

    // The secrets the slots name for the given fields: the ones new values replace.
    private static List<StoredSecret> Named(ConnectionCredentialSlots slots, IEnumerable<string> fields)
    {
        if (slots.SecretManagerName is not { } manager)
            return [];

        return fields
            .Select(field => slots.References.TryGetValue(field, out var reference) ? reference : null)
            .Where(reference => !string.IsNullOrEmpty(reference))
            .Select(reference => new StoredSecret(manager, reference!))
            .ToList();
    }

    // The secrets the connection named before the request and names no more after it.
    private static List<StoredSecret> Dropped(
        IConnectionType connectionType, ConnectionCredentialSlots? before, IConnectionImplementationConfiguration after)
    {
        if (before is null)
            return [];

        var now = connectionType.CredentialSlots(after);
        List<string> named = now.IsFailure
            ? []
            : now.Value!.References.Values.Where(reference => !string.IsNullOrEmpty(reference)).Select(reference => reference!).ToList();
        var gone = before.Fields.Where(field =>
            before.References.TryGetValue(field, out var reference)
            && !string.IsNullOrEmpty(reference)
            && !named.Contains(reference!, StringComparer.Ordinal));
        return Named(before, gone);
    }
}
