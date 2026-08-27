using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManager;

/// <summary>
/// Base class for secret management services.
/// </summary>
/// <typeparam name="TSecretCommand">The secret managementCommand type.</typeparam>
/// <typeparam name="TSecretManagerConfiguration">The secret management configuration type.</typeparam>
/// <typeparam name="TSecretManagerService">The concrete secret management service type for logging category.</typeparam>
[ExcludeFromCodeCoverage] // Excluded: requires real vault/secret store connection
public abstract class SecretManagerServiceBase<TSecretCommand, TSecretManagerConfiguration, TSecretManagerService>
    : ServiceBase<TSecretCommand, TSecretManagerConfiguration, TSecretManagerService>, ISecretManager
    where TSecretCommand : IGenericCommand, ISecretManagerCommand
    // Why: After config-split, typed body configs implement ISecretManagerImplementationConfiguration directly.
    where TSecretManagerConfiguration : class, ISecretManagerImplementationConfiguration
    where TSecretManagerService : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerServiceBase{TSecretCommand, TSecretManagerConfiguration, TSecretManagerService}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for the concrete service type.</param>
    /// <param name="configuration">The secret management configuration.</param>
    protected SecretManagerServiceBase(ILogger<TSecretManagerService> logger, TSecretManagerConfiguration configuration)
        : base(logger, configuration)
    {
    }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<object?>> Execute(ISecretManagerCommand managementCommand, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IGenericResult<TResult>> Execute<TResult>(ISecretManagerCommand<TResult> managementCommand, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IGenericResult> ExecuteBatch(IReadOnlyList<ISecretManagerCommand> commands, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract IGenericResult ValidateCommand(ISecretManagerCommand managementCommand);
}