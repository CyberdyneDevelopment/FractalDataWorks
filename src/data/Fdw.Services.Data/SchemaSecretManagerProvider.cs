using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// The secret managers a host declares in <c>configurationSchema.json</c>, read by name and nothing else (FDW-6360).
/// </summary>
/// <remarks>
/// <para>
/// It exists so a connection opened before any configuration store does can take its secret from a manager: it is built once, from the
/// declaration, with no gateway, and it is handed to the connection kind's own provider. It is not registered in the container: the container's
/// <see cref="ISecretManagerProvider"/> reads its managers from the configuration store, and nothing that opens that store may touch it.
/// </para>
/// <para>
/// It is read-only. The managers are the ones the schema declares and no others; a request to register one, or to change what is declared, is a
/// coded refusal, and a name the schema does not declare is a coded refusal naming it. A host that declares none gets an empty one, which answers
/// every name with that refusal; nothing is returned in its place.
/// </para>
/// </remarks>
public sealed class SchemaSecretManagerProvider : ISecretManagerProvider
{
    private readonly IReadOnlyDictionary<string, ISecretManager> _managers;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="SchemaSecretManagerProvider"/> class.</summary>
    /// <param name="managers">The managers the schema declares, by the name it gives them.</param>
    /// <param name="logger">The logger.</param>
    public SchemaSecretManagerProvider(IReadOnlyDictionary<string, ISecretManager> managers, ILogger<SchemaSecretManagerProvider>? logger = null)
    {
        _managers = new Dictionary<string, ISecretManager>(managers, StringComparer.OrdinalIgnoreCase);
        _logger = logger ?? NullLogger<SchemaSecretManagerProvider>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<ISecretManager>> Get(string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_managers.TryGetValue(name ?? string.Empty, out var manager)
            ? GenericResult<ISecretManager>.Success(manager)
            : GenericResult<ISecretManager>.Failure(ConfigurationGatewayProviderLog.SecretManagerNotDeclared(_logger, name ?? string.Empty)));
    }

    /// <inheritdoc />
    public Task<IGenericResult<ISecretManager>> Get(Guid id, CancellationToken cancellationToken = default)
        => Refused<ISecretManager>($"Get by id {id:D}", cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<ISecretManager>>> Get(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GenericResult<IReadOnlyList<ISecretManager>>.Success([.. _managers.Values]));
    }

    /// <inheritdoc />
    public Task<IGenericResult<ISecretManager>> Get(IGenericConfiguration configuration, CancellationToken cancellationToken = default)
        => Refused<ISecretManager>("Get by configuration", cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<T>> Get<T>(string name, CancellationToken cancellationToken = default)
        where T : IGenericService
    {
        var found = await Get(name, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null) return found.ToNewResult<T>();

        return found.Value is T typed
            ? GenericResult<T>.Success(typed)
            : GenericResult<T>.Failure(ConfigurationGatewayProviderLog.SchemaSecretManagersAreReadOnly(_logger, $"Get<{typeof(T).Name}> of '{name}'"));
    }

    /// <inheritdoc />
    public Task<IGenericResult<T>> Get<T>(Guid id, CancellationToken cancellationToken = default)
        where T : IGenericService
        => Refused<T>($"Get by id {id:D}", cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<T>>> Get<T>(CancellationToken cancellationToken = default)
        where T : IGenericService
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GenericResult<IReadOnlyList<T>>.Success([.. _managers.Values.OfType<T>()]));
    }

    /// <inheritdoc />
    public IGenericResult Register(
        string implementation,
        Func<IImplementationServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>> implementationProviderFactory)
        => GenericResult.Failure(ConfigurationGatewayProviderLog.SchemaSecretManagersAreReadOnly(_logger, $"Register '{implementation}'"));

    /// <inheritdoc />
    public IGenericResult Register(IDomainConfigurationProvider<ISecretManagerImplementationConfiguration> domainConfigurationProvider)
        => GenericResult.Failure(ConfigurationGatewayProviderLog.SchemaSecretManagersAreReadOnly(_logger, "Register a configuration provider"));

    private Task<IGenericResult<T>> Refused<T>(string operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GenericResult<T>.Failure(ConfigurationGatewayProviderLog.SchemaSecretManagersAreReadOnly(_logger, operation)));
    }
}
