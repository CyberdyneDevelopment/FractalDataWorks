using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Processors;

/// <summary>
/// Core interface for asynchronous command processors.
/// Use when processing requires async operations (token acquisition, API calls, secret resolution).
/// </summary>
/// <typeparam name="TCommand">The type being processed.</typeparam>
/// <typeparam name="TContext">The processing context (typically a readonly record struct).</typeparam>
/// <remarks>
/// <para>
/// Async processors follow the same patterns as <see cref="IProcessor{TCommand, TContext}"/>
/// but support async operations in the Process method.
/// </para>
/// <para>
/// Use IAsyncProcessor when the processing operation needs to:
/// <list type="bullet">
/// <item>Acquire tokens from identity providers</item>
/// <item>Resolve secrets from external secret managers</item>
/// <item>Make HTTP calls for credential validation</item>
/// <item>Perform any I/O-bound operation</item>
/// </list>
/// </para>
/// </remarks>
public interface IAsyncProcessor<TCommand, TContext>
{
    /// <summary>
    /// Gets a value indicating whether this is the Empty/NotFound sentinel.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets the list of required context properties for this processor.
    /// </summary>
    IReadOnlyList<string> RequiredProperties { get; }

    /// <summary>
    /// Validates that the context has all required properties for this processor.
    /// </summary>
    /// <param name="context">The processing context to validate.</param>
    /// <returns>Success if valid, Failure with error messages if not.</returns>
    IGenericResult Validate(TContext context);

    /// <summary>
    /// Asynchronously processes the command using the provided context.
    /// </summary>
    /// <param name="command">The command to process.</param>
    /// <param name="context">The processing context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The processed command wrapped in a result, or failure information.</returns>
    /// <remarks>
    /// <para>
    /// Implementations should respect the cancellation token for long-running operations.
    /// Validation is synchronous and should be called before async processing begins.
    /// </para>
    /// </remarks>
    Task<IGenericResult<TCommand>> Process(
        TCommand command,
        TContext context,
        CancellationToken cancellationToken = default);
}
