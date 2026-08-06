using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Messages;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Results;

/// <summary>
/// Basic implementation of IGenericResult.
/// </summary>
public class GenericResult : IGenericResult
{
    private readonly List<IGenericMessage> _messages = [];

    /// <summary>
    /// Constructor for GenericResult
    /// </summary>
    /// <param name="isSuccess"></param>
    /// <param name="message"></param>
    protected GenericResult(bool isSuccess, string? message = null)
    {
        IsSuccess = isSuccess;
        Status = isSuccess ? ResultStatuses.ByName("Success") : ResultStatuses.ByName("Failure");
        if (!string.IsNullOrEmpty(message))
        {
            _messages.Add(new GenericMessage(message!));
        }
    }

    /// <summary>
    /// Constructor for GenericResult with IGenericMessage
    /// </summary>
    /// <param name="isSuccess"></param>
    /// <param name="message"></param>
    protected GenericResult(bool isSuccess, IGenericMessage? message)
    {
        IsSuccess = isSuccess;
        Status = isSuccess ? ResultStatuses.ByName("Success") : ResultStatuses.ByName("Failure");
        if (message != null && !string.IsNullOrEmpty(message.Message))
        {
            _messages.Add(message);
        }
    }

    /// <summary>
    /// Constructor for GenericResult with multiple messages
    /// </summary>
    /// <param name="isSuccess"></param>
    /// <param name="messages"></param>
    protected GenericResult(bool isSuccess, IEnumerable<IGenericMessage>? messages)
    {
        IsSuccess = isSuccess;
        Status = isSuccess ? ResultStatuses.ByName("Success") : ResultStatuses.ByName("Failure");
        if (messages != null)
        {
            _messages.AddRange(messages);
        }
    }

    /// <summary>
    /// Constructor for GenericResult with explicit status
    /// </summary>
    /// <param name="status">The result status.</param>
    /// <param name="messages">Optional messages.</param>
    protected GenericResult(IResultStatus status, IEnumerable<IGenericMessage>? messages = null)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
        IsSuccess = status.IsSuccess;
        if (messages != null)
        {
            _messages.AddRange(messages);
        }
    }

    /// <summary>
    /// Constructor for GenericResult with IResultCode
    /// </summary>
    /// <param name="code">The result code.</param>
    /// <param name="details">Optional details to format into the message.</param>
    protected GenericResult(IResultCode code, IResultDetails? details = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Details = details;
        IsSuccess = code.Severity.IsSuccess;
        Status = code.Severity.IsSuccess ? ResultStatuses.ByName("Success") : ResultStatuses.ByName("Failure");

        var formattedMessage = code.FormatMessage(details);
        _messages.Add(new GenericMessage(
            code.Severity.IsSuccess ? MessageSeverity.Information : MessageSeverity.Error,
            formattedMessage,
            code.Code,
            code.Domain));
    }

    /// <summary>
    /// Constructor for GenericResult with IResultCode and inner result for chaining.
    /// </summary>
    /// <param name="code">The result code.</param>
    /// <param name="innerResult">The inner result to wrap.</param>
    /// <param name="details">Optional details to format into the message.</param>
    protected GenericResult(IResultCode code, IGenericResult innerResult, IResultDetails? details = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        InnerResult = innerResult ?? throw new ArgumentNullException(nameof(innerResult));
        Details = details;
        IsSuccess = code.Severity.IsSuccess;
        Status = code.Severity.IsSuccess ? ResultStatuses.ByName("Success") : ResultStatuses.ByName("Failure");

        var formattedMessage = code.FormatMessage(details);
        _messages.Add(new GenericMessage(
            code.Severity.IsSuccess ? MessageSeverity.Information : MessageSeverity.Error,
            formattedMessage,
            code.Code,
            code.Domain));

        // Copy messages from inner result for visibility
        if (innerResult.Messages.Count > 0)
        {
            _messages.AddRange(innerResult.Messages);
        }
    }

    /// <summary>
    /// Constructor that creates a result by copying all metadata from an existing result.
    /// Preserves Code, InnerResult, Details, and Messages for cross-type result conversion.
    /// </summary>
    /// <param name="source">The source result to copy metadata from.</param>
    protected GenericResult(IGenericResult source)
    {
        IsSuccess = source.IsSuccess;
        Status = source.Status;
        Code = source.Code;
        InnerResult = source.InnerResult;
        Details = source.Details;
        if (source.Messages.Count > 0)
        {
            _messages.AddRange(source.Messages);
        }
    }


    /// <inheritdoc/>
    public virtual bool IsSuccess { get; }

    /// <inheritdoc/>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Returns a value indicating whether there is an error
    /// </summary>
    public bool Error => !IsSuccess;

    /// <summary>
    /// Returns a value indicating whether there is a message;
    /// </summary>
    public virtual bool IsEmpty => _messages.Count == 0;

    /// <summary>
    /// Provides collection of messages associated with the result
    /// </summary>
    public IReadOnlyList<IGenericMessage> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Gets the most recent message (LIFO - Last In, First Out)
    /// </summary>
    public string? CurrentMessage => _messages.LastOrDefault()?.Message;


    /// <summary>
    /// Gets the result code if this result was created with one.
    /// </summary>
    public IResultCode? Code { get; }

    /// <summary>
    /// Gets the result details if this result was created with them.
    /// </summary>
    public IResultDetails? Details { get; }

    /// <summary>
    /// Gets the inner result that this result wraps, if any.
    /// Used for error chain propagation.
    /// </summary>
    public IGenericResult? InnerResult { get; }

    /// <summary>
    /// Gets the chain of result codes from outermost to innermost.
    /// Provides a flattened view of the error chain for iteration.
    /// </summary>
    public IReadOnlyList<IResultCode> CodeChain
    {
        get
        {
            var codes = new List<IResultCode>();
            IGenericResult? current = this;

            while (current != null)
            {
                if (current.Code != null)
                {
                    codes.Add(current.Code);
                }
                current = current.InnerResult;
            }

            return codes.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the root cause result (the innermost result in the chain).
    /// </summary>
    public IGenericResult RootCause
    {
        get
        {
            IGenericResult current = this;
            while (current.InnerResult != null)
            {
                current = current.InnerResult;
            }
            return current;
        }
    }

    /// <inheritdoc/>
    public IResultStatus Status { get; }


    /// <summary>
    /// Adds a message to this result's message collection.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <ExcludedFromCoverage>Protected extension point for derived classes - not currently used</ExcludedFromCoverage>
    [ExcludeFromCodeCoverage]
    protected void AddMessage(IGenericMessage message)
    {
        _messages.Add(message);
    }

    /// <summary>
    /// Adds multiple messages to this result's message collection.
    /// </summary>
    /// <param name="messages">The messages to add.</param>
    /// <ExcludedFromCoverage>Protected extension point for derived classes - not currently used</ExcludedFromCoverage>
    [ExcludeFromCodeCoverage]
    protected void AddMessages(IEnumerable<IGenericMessage> messages)
    {
        _messages.AddRange(messages);
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static IGenericResult Success() => new GenericResult(true);

    /// <summary>
    /// Creates a successful result with a message.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult Success(string message) => new GenericResult(true, message);

    /// <summary>
    /// Creates a successful result with an IGenericMessage.
    /// </summary>
    /// <param name="message">The success message object.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult Success(IGenericMessage message) => new GenericResult(true, message);

    /// <summary>
    /// Creates a successful result with any object that implements IGenericMessage.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message that implements IGenericMessage.</typeparam>
    /// <param name="message">The success message object.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult Success<TMessage>(TMessage message) where TMessage : IGenericMessage => new GenericResult(true, message);

    /// <summary>
    /// Creates a successful result with multiple IGenericMessages.
    /// </summary>
    /// <param name="messages">The success message objects.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult Success(IEnumerable<IGenericMessage> messages) => new GenericResult(true, messages);

    /// <summary>
    /// Creates a successful result with multiple IGenericMessages.
    /// </summary>
    /// <param name="messages">The success message objects.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult Success(params IGenericMessage[] messages) => new GenericResult(true, messages);


    /// <summary>
    /// Creates a successful result with a result code.
    /// </summary>
    /// <param name="code">The success result code.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult Success(IResultCode code, IResultDetails? details = null) => new GenericResult(code, details);

    /// <summary>
    /// Creates a successful result with an explicit status and messages.
    /// </summary>
    /// <param name="status">The result status (e.g., SuccessWithWarnings, SuccessAfterRetry, PartialSuccess).</param>
    /// <param name="messages">The messages to include.</param>
    /// <returns>A successful result with the specified status.</returns>
    public static IGenericResult Success(IResultStatus status, params IGenericMessage[] messages) => new GenericResult(status, messages);


    // Failure(string) removed — failures must carry an IGenericMessage or a categorized IResultCode.

    /// <summary>
    /// Creates a failed result with an IGenericMessage.
    /// </summary>
    /// <param name="message">The failure message object.</param>
    /// <returns>A failed result.</returns>
    public static IGenericResult Failure(IGenericMessage message) => new GenericResult(false, message);

    /// <summary>
    /// Creates a failed result with any object that implements IGenericMessage.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message that implements IGenericMessage.</typeparam>
    /// <param name="message">The failure message object.</param>
    /// <returns>A failed result.</returns>
    public static IGenericResult Failure<TMessage>(TMessage message) where TMessage : IGenericMessage => new GenericResult(false, message);

    /// <summary>
    /// Creates a failed result with multiple IGenericMessages.
    /// </summary>
    /// <param name="messages">The failure message objects.</param>
    /// <returns>A failed result.</returns>
    public static IGenericResult Failure(IEnumerable<IGenericMessage> messages) => new GenericResult(false, messages);

    /// <summary>
    /// Creates a failed result with multiple IGenericMessages.
    /// </summary>
    /// <param name="messages">The failure message objects.</param>
    /// <returns>A failed result.</returns>
    public static IGenericResult Failure(params IGenericMessage[] messages) => new GenericResult(false, messages);


    /// <summary>
    /// Creates a failed result with a result code.
    /// </summary>
    /// <param name="code">The failure result code.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result.</returns>
    public static IGenericResult Failure(IResultCode code, IResultDetails? details = null) => new GenericResult(code, details);

    /// <summary>
    /// Creates a failed result with a result code and logs it.
    /// </summary>
    /// <param name="code">The failure result code.</param>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result.</returns>
    public static IGenericResult Failure(IResultCode code, ILogger logger, IResultDetails? details = null)
    {
        code.Log(logger, details);
        return new GenericResult(code, details);
    }

    /// <summary>
    /// Creates a failed result that chains to an inner result, adding context.
    /// </summary>
    /// <param name="code">The result code for the outer failure.</param>
    /// <param name="innerResult">The inner result to wrap.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result with chain.</returns>
    public static IGenericResult Chain(IResultCode code, IGenericResult innerResult, IResultDetails? details = null) =>
        new GenericResult(code, innerResult, details);

    /// <summary>
    /// Creates a failed result that chains to an inner result, adding context, and logs it.
    /// </summary>
    /// <param name="code">The result code for the outer failure.</param>
    /// <param name="innerResult">The inner result to wrap.</param>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result with chain.</returns>
    public static IGenericResult Chain(IResultCode code, IGenericResult innerResult, ILogger logger, IResultDetails? details = null)
    {
        code.Log(logger, details);
        return new GenericResult(code, innerResult, details);
    }

    /// <inheritdoc/>
    public IGenericResult<TNew> ToNewResult<TNew>(TNew value)
    {
        return IsSuccess
            ? GenericResult<TNew>.FromResult(value, this)
            : GenericResult<TNew>.FromResult(this);
    }

    /// <inheritdoc/>
    public IGenericResult<TNew> ToNewResult<TNew>()
    {
        if (IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful result without providing a value. Use ToNewResult<TNew>(TNew value) instead.");

        return GenericResult<TNew>.FromResult(this);
    }

}

/// <summary>
/// Basic implementation of IGenericResult with a value.
/// </summary>
/// <typeparam name="TResult">The type of the value.</typeparam>
public class GenericResult<TResult> : GenericResult, IGenericResult<TResult>
{
    private readonly TResult _value;
    private readonly bool _hasValue;

    private GenericResult(bool isSuccess, TResult value, string? message = null) : base(isSuccess, message)
    {
        _value = value;
        _hasValue = isSuccess;
    }

    private GenericResult(bool isSuccess, TResult value, IGenericMessage? message) : base(isSuccess, message)
    {
        _value = value;
        _hasValue = isSuccess;
    }

    private GenericResult(bool isSuccess, TResult value, IEnumerable<IGenericMessage>? messages) : base(isSuccess, messages)
    {
        _value = value;
        _hasValue = isSuccess;
    }

    private GenericResult(TResult value, IResultStatus status, IEnumerable<IGenericMessage>? messages = null) : base(status, messages)
    {
        _value = value;
        _hasValue = status.IsSuccess;
    }

    private GenericResult(TResult value, IResultCode code, IResultDetails? details = null) : base(code, details)
    {
        _value = value;
        _hasValue = code.Severity.IsSuccess;
    }

    private GenericResult(TResult value, IResultCode code, IGenericResult innerResult, IResultDetails? details = null)
        : base(code, innerResult, details)
    {
        _value = value;
        _hasValue = code.Severity.IsSuccess;
    }

    private GenericResult(TResult value, IGenericResult source) : base(source)
    {
        _value = value;
        _hasValue = source.IsSuccess;
    }


    /// <summary>
    /// Provides collection of messages associated with the result
    /// </summary>
    public new IReadOnlyList<IGenericMessage> Messages => base.Messages;

    /// <summary>
    /// Returns a value indicating whether it is empty
    /// </summary>
    public override bool IsEmpty => !_hasValue;

    /// <inheritdoc/>
    public TResult Value
    {
        get
        {
            if (!_hasValue)
                throw new InvalidOperationException("Cannot access value of a failed result.");
            return _value;
        }
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult<TResult> Success(TResult value) => new GenericResult<TResult>(true, value);

    /// <summary>
    /// Creates a successful result with a value and message.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult<TResult> Success(TResult value, string message) => new GenericResult<TResult>(true, value, message);

    /// <summary>
    /// Creates a successful result with a value and IGenericMessage.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="message">The success message object.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult<TResult> Success(TResult value, IGenericMessage message) => new GenericResult<TResult>(true, value, message);

    /// <summary>
    /// Creates a successful result with a value and any object that implements IGenericMessage.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message that implements IGenericMessage.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="message">The success message object.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult<TResult> Success<TMessage>(TResult value, TMessage message) where TMessage : IGenericMessage => new GenericResult<TResult>(true, value, message);

    /// <summary>
    /// Creates a successful result with a value and multiple IGenericMessages.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="messages">The success message objects.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult<TResult> Success(TResult value, IEnumerable<IGenericMessage> messages) => new GenericResult<TResult>(true, value, messages);

    /// <summary>
    /// Creates a successful result with a value and multiple IGenericMessages.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="messages">The success message objects.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult<TResult> Success(TResult value, params IGenericMessage[] messages) => new GenericResult<TResult>(true, value, messages);


    /// <summary>
    /// Creates a successful result with a value and result code.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="code">The success result code.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A successful result.</returns>
    public static IGenericResult<TResult> Success(TResult value, IResultCode code, IResultDetails? details = null) => new GenericResult<TResult>(value, code, details);

    /// <summary>
    /// Creates a successful result with a value, explicit status, and messages.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="status">The result status (e.g., SuccessWithWarnings, SuccessAfterRetry, PartialSuccess).</param>
    /// <param name="messages">The messages to include.</param>
    /// <returns>A successful result with the specified status.</returns>
    public static IGenericResult<TResult> Success(TResult value, IResultStatus status, params IGenericMessage[] messages) => new GenericResult<TResult>(value, status, messages);


    // Failure(string) removed — failures must carry an IGenericMessage or a categorized IResultCode.

    /// <summary>
    /// Creates a failed result with an IGenericMessage.
    /// </summary>
    /// <param name="message">The failure message object.</param>
    /// <returns>A failed result.</returns>
    public new static IGenericResult<TResult> Failure(IGenericMessage message) => new GenericResult<TResult>(false, default!, message);

    /// <summary>
    /// Creates a failed result with any object that implements IGenericMessage.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message that implements IGenericMessage.</typeparam>
    /// <param name="message">The failure message object.</param>
    /// <returns>A failed result.</returns>
    public new static IGenericResult<TResult> Failure<TMessage>(TMessage message) where TMessage : IGenericMessage => new GenericResult<TResult>(false, default!, message);

    /// <summary>
    /// Creates a failed result with multiple IGenericMessages.
    /// </summary>
    /// <param name="messages">The failure message objects.</param>
    /// <returns>A failed result.</returns>
    public new static IGenericResult<TResult> Failure(IEnumerable<IGenericMessage> messages) => new GenericResult<TResult>(false, default!, messages);

    /// <summary>
    /// Creates a failed result with multiple IGenericMessages.
    /// </summary>
    /// <param name="messages">The failure message objects.</param>
    /// <returns>A failed result.</returns>
    public new static IGenericResult<TResult> Failure(params IGenericMessage[] messages) => new GenericResult<TResult>(false, default!, messages);


    /// <summary>
    /// Creates a failed result with a result code.
    /// </summary>
    /// <param name="code">The failure result code.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result.</returns>
    public new static IGenericResult<TResult> Failure(IResultCode code, IResultDetails? details = null) => new GenericResult<TResult>(default!, code, details);

    /// <summary>
    /// Creates a failed result with a result code and logs it.
    /// </summary>
    /// <param name="code">The failure result code.</param>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result.</returns>
    public new static IGenericResult<TResult> Failure(IResultCode code, ILogger logger, IResultDetails? details = null)
    {
        code.Log(logger, details);
        return new GenericResult<TResult>(default!, code, details);
    }

    /// <summary>
    /// Creates a failed result that chains to an inner result, adding context.
    /// </summary>
    /// <param name="code">The result code for the outer failure.</param>
    /// <param name="innerResult">The inner result to wrap.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result with chain.</returns>
    public new static IGenericResult<TResult> Chain(IResultCode code, IGenericResult innerResult, IResultDetails? details = null) =>
        new GenericResult<TResult>(default!, code, innerResult, details);

    /// <summary>
    /// Creates a failed result that chains to an inner result, adding context, and logs it.
    /// </summary>
    /// <param name="code">The result code for the outer failure.</param>
    /// <param name="innerResult">The inner result to wrap.</param>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result with chain.</returns>
    public new static IGenericResult<TResult> Chain(IResultCode code, IGenericResult innerResult, ILogger logger, IResultDetails? details = null)
    {
        code.Log(logger, details);
        return new GenericResult<TResult>(default!, code, innerResult, details);
    }


    /// <summary>
    /// Creates a new typed result from an existing result, preserving all metadata
    /// (Code, InnerResult, Details, Messages). The value defaults to <c>default</c>.
    /// </summary>
    /// <param name="source">The source result to copy metadata from.</param>
    /// <returns>A new result preserving all metadata from the source.</returns>
    internal static IGenericResult<TResult> FromResult(IGenericResult source) =>
        new GenericResult<TResult>(default!, source);

    /// <summary>
    /// Creates a new typed result from an existing result with a specified value,
    /// preserving all metadata (Code, InnerResult, Details, Messages).
    /// </summary>
    /// <param name="value">The value for the new result.</param>
    /// <param name="source">The source result to copy metadata from.</param>
    /// <returns>A new result with the given value, preserving all metadata from the source.</returns>
    internal static IGenericResult<TResult> FromResult(TResult value, IGenericResult source) =>
        new GenericResult<TResult>(value, source);


    /// <summary>
    /// Projects the success value of this result into a new shape via <paramref name="mapper"/>;
    /// propagates the failure (with its messages) unchanged when the result is not successful.
    /// </summary>
    /// <typeparam name="TNew">The result type produced by <paramref name="mapper"/>.</typeparam>
    /// <param name="mapper">The transformation applied to the success value.</param>
    /// <returns>A result carrying the mapped value, or the original failure.</returns>
    public IGenericResult<TNew> Map<TNew>(Func<TResult, TNew> mapper)
    {
        if (mapper == null)
            throw new ArgumentNullException(nameof(mapper));

        return IsSuccess
            ? GenericResult<TNew>.Success(mapper(Value))
            : GenericResult<TNew>.Failure(Messages);
    }

    /// <summary>
    /// Pattern-matches on the result state, returning <paramref name="success"/>'s output
    /// when successful or <paramref name="failure"/>'s output (with the current message) otherwise.
    /// </summary>
    /// <typeparam name="T">The output type produced by both branches.</typeparam>
    /// <param name="success">Function invoked with the value when the result is successful.</param>
    /// <param name="failure">Function invoked with the current message when the result is a failure.</param>
    /// <returns>The value produced by whichever branch was selected.</returns>
    public T Match<T>(Func<TResult, T> success, Func<string, T> failure)
    {
        if (success == null)
            throw new ArgumentNullException(nameof(success));
        if (failure == null)
            throw new ArgumentNullException(nameof(failure));

        return IsSuccess ? success(Value) : failure(CurrentMessage ?? string.Empty);
    }
}
