using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;


namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// Main interface for secret management operations in the Fdw framework.
/// Provides a unified facade for secret operations across different provider implementations.
/// </summary>
/// <remarks>
/// This interface abstracts secret management operations using a managementCommand-based pattern,
/// allowing different secret storage providers (AWS Secrets Manager, Azure Key Vault, etc.)
/// to be used interchangeably through a consistent API.
/// </remarks>
public interface ISecretManager : IDisposable, IServiceOption
{
    /// <summary>
    /// Executes a secret management managementCommand.
    /// </summary>
    /// <param name="managementCommand">The managementCommand to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the managementCommand result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="managementCommand"/> is null.</exception>
    /// <remarks>
    /// This is the primary method for executing secret operations. The managementCommand pattern
    /// allows for consistent handling of different operation types while maintaining
    /// flexibility for provider-specific implementations.
    /// </remarks>
    Task<IGenericResult<object?>> Execute(ISecretManagerCommand managementCommand, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a typed secret management managementCommand.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="managementCommand">The managementCommand to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the typed managementCommand result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="managementCommand"/> is null.</exception>
    /// <remarks>
    /// This method provides compile-time type safety for secret operations when the
    /// expected result type is known. It eliminates the need for runtime type checking.
    /// </remarks>
    Task<IGenericResult<TResult>> Execute<TResult>(ISecretManagerCommand<TResult> managementCommand, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple secret commands as a batch operation.
    /// </summary>
    /// <param name="commands">The collection of secret commands to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous batch operation, containing the batch results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commands"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="commands"/> is empty.</exception>
    /// <remarks>
    /// Batch operations allow for efficient processing of multiple secret operations
    /// and may provide transactional guarantees depending on the provider implementation.
    /// If one managementCommand fails, the behavior depends on the provider's batch handling strategy.
    /// </remarks>
    Task<IGenericResult> ExecuteBatch(IReadOnlyList<ISecretManagerCommand> commands, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a secret managementCommand before execution.
    /// </summary>
    /// <param name="managementCommand">The managementCommand to validate.</param>
    /// <returns>A result indicating whether the managementCommand is valid for execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="managementCommand"/> is null.</exception>
    /// <remarks>
    /// This method performs validation of the managementCommand including parameter checking,
    /// access control verification, and provider capability assessment.
    /// It allows pre-flight validation without executing the actual operation.
    /// </remarks>
    IGenericResult ValidateCommand(ISecretManagerCommand managementCommand);
}