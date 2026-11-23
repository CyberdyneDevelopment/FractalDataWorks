using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Logging;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Builds the connections the host declares in its <c>configurationSchema.json</c>, the same way the configuration
/// gateway builds the one it opens.
/// </summary>
/// <remarks>
/// It resolves the connection kind's FACTORY and builds the kind's own implementation provider from it and the secret managers the schema
/// declares, as <see cref="ConfigurationGatewayTypes"/> does for a gateway: one path for every host, and a declared connection that needs a
/// secret reads it from a manager the schema declares. Nothing is cached; the connection wraps a driver that pools underneath.
/// </remarks>
public sealed class DeclaredConnectionProvider : IDeclaredConnectionProvider
{
    private readonly ConfigurationSchema _schema;
    private readonly IServiceProvider _services;
    private readonly ISecretManagerProvider _secretManagers;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<DeclaredConnectionProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeclaredConnectionProvider"/> class.</summary>
    /// <param name="schema">The host's bound <c>configurationSchema.json</c>.</param>
    /// <param name="services">Supplies the factory registered for each connection kind.</param>
    /// <param name="secretManagers">The secret managers the schema declares; empty when it declares none.</param>
    /// <param name="loggerFactory">Where the kinds' providers log their refusals.</param>
    /// <param name="logger">The logger.</param>
    public DeclaredConnectionProvider(
        ConfigurationSchema schema,
        IServiceProvider services,
        ISecretManagerProvider secretManagers,
        ILoggerFactory? loggerFactory,
        ILogger<DeclaredConnectionProvider>? logger = null)
    {
        _schema = schema;
        _services = services;
        _secretManagers = secretManagers;
        _loggerFactory = loggerFactory;
        _logger = logger ?? NullLogger<DeclaredConnectionProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDataConnection>> Get(string connectionName, CancellationToken cancellationToken = default)
    {
        var declared = _schema.Connections.FirstOrDefault(
            c => string.Equals(c.Name, connectionName, StringComparison.OrdinalIgnoreCase));
        if (declared is null)
            return GenericResult<IDataConnection>.Failure(
                ConfigurationGatewayProviderLog.ConnectionNotDeclared(_logger, connectionName));

        var factory = FactoryFor(connectionName, declared);
        if (factory.IsFailure)
            return factory.ToNewResult<IDataConnection>();

        var provider = ProviderFor(connectionName, declared, factory.Value!);
        if (provider.IsFailure || provider.Value is not { } connectionProvider)
            return provider.ToNewResult<IDataConnection>();

        var created = await connectionProvider.Create(declared, cancellationToken).ConfigureAwait(false);
        // The factory's own failure already names what went wrong; it travels unchanged.
        if (created.IsFailure)
            return created.ToNewResult<IDataConnection>();

        if (created.Value is not IDataConnection connection)
            return GenericResult<IDataConnection>.Failure(
                ConfigurationGatewayLog.ConnectionCreationFailed(
                    _logger, connectionName, "the factory built a connection that is not an IDataConnection"));

        return GenericResult<IDataConnection>.Success(connection);
    }

    // The kind's own provider, built from its factory and the declared secret managers, or why the kind cannot build one.
    private IGenericResult<Fdw.Services.Abstractions.IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>> ProviderFor(
        string connectionName, IConnectionImplementationConfiguration declared, IConnectionFactory factory)
        => ConnectionTypes.ByName(declared.Implementation).ImplementationProvider(factory, _secretManagers, _loggerFactory);

    // The factory registered for the declared connection's kind, or why there is none.
    private IGenericResult<IConnectionFactory> FactoryFor(string connectionName, IConnectionImplementationConfiguration declared)
    {
        if (string.IsNullOrWhiteSpace(declared.Implementation))
            return GenericResult<IConnectionFactory>.Failure(
                ConfigurationGatewayProviderLog.ConnectionDeclaresNoKind(_logger, connectionName));

        var connectionType = ConnectionTypes.ByName(declared.Implementation);
        if (ReferenceEquals(connectionType, ConnectionTypes.NotFound))
            return GenericResult<IConnectionFactory>.Failure(
                ConfigurationGatewayProviderLog.ConnectionKindNotRegistered(_logger, connectionName, declared.Implementation));

        if (_services.GetService(connectionType.FactoryType) is not IConnectionFactory factory)
            return GenericResult<IConnectionFactory>.Failure(
                ConfigurationGatewayProviderLog.ConnectionFactoryUnavailable(_logger, connectionName, connectionType.FactoryType.Name));

        return GenericResult<IConnectionFactory>.Success(factory);
    }
}
