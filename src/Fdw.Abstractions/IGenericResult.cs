using System.Collections.Generic;
using Fdw.Messages;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Represents a result that can be either success or failure.
/// </summary>
public interface IGenericResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    bool IsFailure { get; }

    /// <summary>
    /// Gets a value indicating whether this represents an empty result
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets a value indicating whether this result represents an error.
    /// </summary>
    bool Error { get; }

    /// <summary>
    /// Gets the most recent message (LIFO - Last In, First Out).
    /// </summary>
    string? CurrentMessage { get; }

    /// <summary>
    /// Gets the collection of messages associated with this result.
    /// </summary>
    IReadOnlyList<IGenericMessage> Messages { get; }

    /// <summary>
    /// Gets the result code if this result was created with one.
    /// </summary>
    IResultCode? Code { get; }

    /// <summary>
    /// Gets the result details if this result was created with them.
    /// </summary>
    IResultDetails? Details { get; }

    /// <summary>
    /// Gets the inner result that this result wraps, if any.
    /// Used for error chain propagation.
    /// </summary>
    IGenericResult? InnerResult { get; }

    /// <summary>
    /// Gets the chain of result codes from outermost to innermost.
    /// Provides a flattened view of the error chain for iteration.
    /// </summary>
    IReadOnlyList<IResultCode> CodeChain { get; }

    /// <summary>
    /// Gets the root cause result (the innermost result in the chain).
    /// </summary>
    IGenericResult RootCause { get; }

    /// <summary>
    /// Gets the result status indicating outcome nuance (Success, SuccessWithWarnings, Failure, etc.).
    /// </summary>
    IResultStatus Status { get; }

    /// <summary>
    /// Creates a new typed result, preserving the success/failure state and all metadata
    /// (Code, InnerResult, Details, Messages).
    /// If the source result is a failure, the value is ignored and a failure result is returned.
    /// </summary>
    /// <typeparam name="TNew">The type of the new result value.</typeparam>
    /// <param name="value">The value for the new result (used only when the source is successful).</param>
    /// <returns>A new result of type <typeparamref name="TNew"/> with the same state, code, chain, and messages.</returns>
    IGenericResult<TNew> ToNewResult<TNew>(TNew value);

    /// <summary>
    /// Creates a new typed failure result, preserving all metadata from the source result
    /// (Code, InnerResult, Details, Messages).
    /// Use this for cross-type failure propagation without providing a value.
    /// </summary>
    /// <typeparam name="TNew">The type of the new result value.</typeparam>
    /// <returns>A failure result of type <typeparamref name="TNew"/> with the same code, chain, and messages.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when called on a successful result.</exception>
    IGenericResult<TNew> ToNewResult<TNew>();
}

/// <summary>
/// Represents a result that can be either success or failure with a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface IGenericResult<out T> : IGenericResult
{
    /// <summary>
    /// Gets the result value if successful.
    /// </summary>
    T? Value { get; }
}
