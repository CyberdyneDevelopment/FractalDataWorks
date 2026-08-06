using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.FileSystem.Abstractions.Logging;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.FileSystem;

/// <summary>
/// Factory for creating <see cref="FileSystemConnection"/> instances.
/// Validates Root is non-empty and the directory exists before building the connection.
/// </summary>
public sealed class FileSystemConnectionFactory : IFileSystemConnectionFactory
{
    private readonly ILogger<FileSystemConnectionFactory> _logger;
    private readonly ILogger<FileSystemConnection> _connectionLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConnectionFactory"/> class.
    /// </summary>
    public FileSystemConnectionFactory(
        ILogger<FileSystemConnectionFactory> logger,
        ILogger<FileSystemConnection> connectionLogger)
    {
        _logger = logger ?? NullLogger<FileSystemConnectionFactory>.Instance;
        _connectionLogger = connectionLogger ?? NullLogger<FileSystemConnection>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration)
    {
        // Why: After config-split DefaultConnectionProvider passes a composed ConnectionConfiguration
        // header. Extract connectionName and typed body from the header.
        if (configuration is ConnectionConfiguration header
            && header.Configuration is FileSystemConnectionConfiguration typedBody)
            return CreateInternal(typedBody, header.Name);

        if (configuration is FileSystemConnectionConfiguration fsConfig)
            return CreateInternal(fsConfig, string.Empty);

        return GenericResult<IGenericConnection>.Failure(
            FileSystemConnectionLog.FactoryValidationFailed(
                _logger,
                configuration?.GetType().Name ?? "null",
                $"Expected FileSystemConnectionConfiguration but got {configuration?.GetType().Name ?? "null"}"));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        ISecretManager? secretManager,
        CancellationToken cancellationToken = default)
    {
        // Why: FileSystem connections have no secrets to resolve. Delegate to sync Create.
        return Task.FromResult(Create(configuration));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // Why: FileSystemConnectionConfiguration declares no AuthenticationType and there is no
        // FileSystem authentication TypeCollection, so there is no secret to resolve and nothing for a
        // secret-manager provider to do. A remote transport (SFTP) would add an authentication type
        // first; the provider becomes a constructor dependency at that point, not before.
        return Task.FromResult(Create(configuration));
    }

    /// <inheritdoc />
    public IGenericResult<IGenericConnection> Create(FileSystemConnectionConfiguration configuration)
        // Why: Direct typed-body path has no ConnectionConfiguration header — name not available.
        => CreateInternal(configuration, string.Empty);

    private IGenericResult<IGenericConnection> CreateInternal(
        FileSystemConnectionConfiguration configuration, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(configuration.Root))
            return GenericResult<IGenericConnection>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    _logger, connectionName.Length > 0 ? connectionName : configuration.ConnectionId.ToString(),
                    "Root directory is required but was empty or whitespace"));

        var canonicalRoot = Path.GetFullPath(configuration.Root);

        if (!Directory.Exists(canonicalRoot))
            return GenericResult<IGenericConnection>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    _logger, connectionName.Length > 0 ? connectionName : configuration.ConnectionId.ToString(),
                    $"Root directory does not exist: {canonicalRoot}"));

        return GenericResult<IGenericConnection>.Success(
            new FileSystemConnection(configuration, _connectionLogger));
    }

    #region IServiceFactory Implementation

    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
            return result.ToNewResult<T>();

        if (result.Value is T typedResult)
            return GenericResult<T>.Success(typedResult);

        return GenericResult<T>.Failure(
            FileSystemConnectionLog.FactoryValidationFailed(
                _logger, configuration?.GetType().Name ?? "null",
                $"Connection is not assignable to {typeof(T).Name}"));
    }

    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
            return result.ToNewResult<IGenericService>();

        return GenericResult<IGenericService>.Success(result.Value);
    }

    #endregion
}
