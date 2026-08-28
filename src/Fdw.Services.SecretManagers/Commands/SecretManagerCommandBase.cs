using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions.Commands;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Logging;
using Fdw.Services.SecretManagers.Logging;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// Base class for all secret management commands providing provider-agnostic secret operations.
/// </summary>
/// <remarks>
/// SecretManagerCommandBase represents universal secret operations that can be translated to provider-specific
/// implementations. This enables the same commands to work across Azure Key Vault, HashiCorp Vault,
/// AWS Secrets Manager, and other secret providers.
/// </remarks>
public abstract class SecretManagerCommandBase : ISecretManagerCommand
{
    private readonly Dictionary<string, object?> _parameters;
    private readonly Dictionary<string, object> _metadata;
    private readonly ILogger<SecretManagerCommandBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerCommandBase"/> class.
    /// </summary>
    /// <param name="commandType">The type of managementCommand (GetSecret, SetSecret, DeleteSecret, ListSecrets).</param>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="expectedResultType">The expected result type.</param>
    /// <param name="parameters">ManagementCommand parameters.</param>
    /// <param name="metadata">Additional managementCommand metadata.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <param name="logger">Optional logger for validation and diagnostic messages.</param>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    protected SecretManagerCommandBase(
        string commandType,
        string? container,
        string? secretKey,
        Type expectedResultType,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        TimeSpan? timeout = null,
        ILogger<SecretManagerCommandBase>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(commandType))
        {
            SecretManagerCommandBaseLog.RequiredValueMissing(
                logger ?? NullLogger<SecretManagerCommandBase>.Instance, nameof(commandType));
            throw new ArgumentException("ManagementCommand type cannot be null or empty.", nameof(commandType));
        }

        var commandGuid = Guid.NewGuid();
        BaseCommandId = commandGuid;
        CommandId = commandGuid.ToString("D");
        CorrelationId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Configuration = null; // Secret commands don't typically carry configuration
        CommandType = commandType;
        Container = container;
        SecretKey = secretKey;
        if (expectedResultType is null)
        {
            SecretManagerCommandBaseLog.RequiredValueMissing(
                logger ?? NullLogger<SecretManagerCommandBase>.Instance, nameof(expectedResultType));
            throw new ArgumentNullException(nameof(expectedResultType));
        }

        ExpectedResultType = expectedResultType;
        Timeout = timeout;
        _logger = logger ?? NullLogger<SecretManagerCommandBase>.Instance;

        _parameters = parameters != null
            ? new Dictionary<string, object?>(parameters, StringComparer.Ordinal)
            : new Dictionary<string, object?>(StringComparer.Ordinal);

        _metadata = metadata != null
            ? new Dictionary<string, object>(metadata, StringComparer.Ordinal)
            : new Dictionary<string, object>(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    Guid IGenericCommand.CommandId => BaseCommandId;

    /// <inheritdoc/>
    DateTime IGenericCommand.CreatedAt => CreatedAt;

    /// <inheritdoc/>
    string IGenericCommand.CommandType => CommandType;

    /// <inheritdoc/>
    string IGenericCommand.Category => "SecretManagement";

    /// <summary>
    /// Gets the correlation identifier for tracking related operations.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Gets the timestamp when this command was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the configuration associated with this command.
    /// </summary>
    public IGenericConfiguration? Configuration { get; }

    /// <summary>
    /// Gets the base managementCommand identifier as a Guid.
    /// </summary>
    protected Guid BaseCommandId { get; }

    /// <inheritdoc/>
    string ISecretManagerCommand.CommandId => CommandId;

    /// <summary>
    /// Gets the managementCommand identifier as a string.
    /// </summary>
    public string CommandId { get; }

    /// <inheritdoc/>
    public string CommandType { get; }

    /// <inheritdoc/>
    public string? Container { get; }

    /// <inheritdoc/>
    public string? SecretKey { get; }

    /// <inheritdoc/>
    public Type ExpectedResultType { get; }

    /// <inheritdoc/>
    public TimeSpan? Timeout { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Metadata => _metadata;

    /// <inheritdoc/>
    public abstract bool IsSecretModifying { get; }

    /// <inheritdoc/>
    public virtual IGenericResult Validate()
    {
        SecretManagerCommandBaseLog.Validating(_logger, CommandType);

        var errors = new List<ValidationFailure>();

        // Validate managementCommand type
        if (string.IsNullOrWhiteSpace(CommandType))
        {
            errors.Add(new ValidationFailure(nameof(CommandType), "ManagementCommand type cannot be null or empty."));
        }

        // Validate secret key for operations that require it
        if (RequiresSecretKey() && string.IsNullOrWhiteSpace(SecretKey))
        {
            errors.Add(new ValidationFailure(nameof(SecretKey), "Secret key is required for this operation."));
        }

        // Validate container for operations that require it
        if (RequiresContainer() && string.IsNullOrWhiteSpace(Container))
        {
            errors.Add(new ValidationFailure(nameof(Container), "Container is required for this operation."));
        }

        // Validate parameters for modifying operations
        if (IsSecretModifying && !ValidateModifyingParameters())
        {
            errors.Add(new ValidationFailure(nameof(Parameters), "Invalid parameters for secret modifying operation."));
        }

        var validationResult = new ValidationResult(errors);
        if (validationResult.IsValid)
        {
            SecretManagerCommandBaseLog.ValidationPassed(_logger, CommandType);
            return GenericResult.Success();
        }

        var errorMessage = string.Join("; ", errors.Select(e => e.ErrorMessage));
        return GenericResult.Failure(SecretManagerLogger.ValidationFailed(_logger, errorMessage));
    }

    /// <inheritdoc/>
    public virtual ISecretManagerCommand WithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return CreateCopyWithParameters(newParameters);
    }

    /// <inheritdoc/>
    public virtual ISecretManagerCommand WithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return CreateCopyWithMetadata(newMetadata);
    }

    /// <summary>
    /// Determines whether this managementCommand type requires a secret key.
    /// </summary>
    /// <returns><c>true</c> if a secret key is required; otherwise, <c>false</c>.</returns>
    protected virtual bool RequiresSecretKey()
    {
        return CommandType switch
        {
            "GetSecret" => true,
            "SetSecret" => true,
            "DeleteSecret" => true,
            "GetSecretVersions" => true,
            "ListSecrets" => false,
            _ => true
        };
    }

    /// <summary>
    /// Determines whether this managementCommand type requires a container.
    /// </summary>
    /// <returns><c>true</c> if a container is required; otherwise, <c>false</c>.</returns>
    protected virtual bool RequiresContainer()
    {
        // Most operations require a container/vault specification
        return true;
    }

    /// <summary>
    /// Validates parameters for secret modifying operations.
    /// </summary>
    /// <returns><c>true</c> if parameters are valid; otherwise, <c>false</c>.</returns>
    protected virtual bool ValidateModifyingParameters()
    {
        if (!IsSecretModifying)
            return true;

        // For SetSecret operations, ensure SecretValue parameter exists
        if (string.Equals(CommandType, "SetSecret", StringComparison.Ordinal))
        {
            return Parameters.ContainsKey(nameof(SecretValue)) && Parameters[nameof(SecretValue)] != null;
        }

        return true;
    }

    /// <summary>
    /// Creates a copy of this managementCommand with new parameters.
    /// </summary>
    /// <param name="newParameters">The new parameters.</param>
    /// <returns>A new managementCommand instance with the specified parameters.</returns>
    protected abstract ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters);

    /// <summary>
    /// Creates a copy of this managementCommand with new metadata.
    /// </summary>
    /// <param name="newMetadata">The new metadata.</param>
    /// <returns>A new managementCommand instance with the specified metadata.</returns>
    protected abstract ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata);
}