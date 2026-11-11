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
    // Why lazy: most results carry no message (every plain Success and every value-only
    // Success<T>), and every service, gateway, translator and provider call returns one, so an
    // eagerly allocated empty list was a second heap object on each of them.
    private List<IGenericMessage>? _messages;

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
            EnsureMessages().Add(new GenericMessage(message!));
        }

        RequireMessageOnFailure();
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
            EnsureMessages().Add(message);
        }

        RequireMessageOnFailure();
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
            AppendMessages(messages);
        }

        RequireMessageOnFailure();
    }

    /// <summary>
    /// Constructor for GenericResult with explicit status
    /// </summary>
    /// <param name="status">The result status.</param>
    /// <param name="messages">Optional messages.</param>
    protected GenericResult(IResultStatus status, IEnumerable<IGenericMessage>? messages = null)
    {
        Status = status;
        IsSuccess = status.IsSuccess;
        if (messages != null)
        {
            AppendMessages(messages);
        }

        RequireMessageOnFailure();
    }

    /// <summary>
    /// Constructor for GenericResult with IResultCode
    /// </summary>
    /// <param name="code">The result code.</param>
    /// <param name="details">Optional details to format into the message.</param>
    protected GenericResult(IResultCode code, IResultDetails? details = null)
        : this(code, SeveritySucceeded(code), details)
    {
    }

    /// <summary>
    /// Constructor for GenericResult with IResultCode and the outcome the caller states.
    /// </summary>
    /// <param name="code">The result code.</param>
    /// <param name="isSuccess">Whether the call succeeded. The caller says so; the code's severity does not decide it (FDW-3463).</param>
    /// <param name="details">Optional details to format into the message.</param>
    protected GenericResult(IResultCode code, bool isSuccess, IResultDetails? details = null)
    {
        Code = code;
        Details = details;
        IsSuccess = isSuccess;
        Status = isSuccess ? ResultStatuses.ByName("Success") : ResultStatuses.ByName("Failure");

        var formattedMessage = code.FormatMessage(details);
        EnsureMessages().Add(new GenericMessage(
            isSuccess ? MessageSeverity.Information : MessageSeverity.Error,
            formattedMessage,
            code.Code,
            code.Domain));
    }

    // Why a helper: only Success(code) asks the code's severity for the outcome.
    private protected static bool SeveritySucceeded(IResultCode code) =>
        code.Severity.IsSuccess;

    /// <summary>
    /// Constructor for GenericResult with IResultCode and inner result for chaining.
    /// </summary>
    /// <param name="code">The result code.</param>
    /// <param name="innerResult">The inner result to wrap.</param>
    /// <param name="details">Optional details to format into the message.</param>
    protected GenericResult(IResultCode code, IGenericResult innerResult, IResultDetails? details = null)
        : this(code, innerResult, SeveritySucceeded(code), details)
    {
    }

    /// <summary>
    /// Constructor for GenericResult with IResultCode, an inner result for chaining, and the outcome the caller states.
    /// </summary>
    /// <param name="code">The result code.</param>
    /// <param name="innerResult">The inner result to wrap.</param>
    /// <param name="isSuccess">Whether the call succeeded. The caller says so; the code's severity does not decide it (FDW-3463).</param>
    /// <param name="details">Optional details to format into the message.</param>
    protected GenericResult(IResultCode code, IGenericResult innerResult, bool isSuccess, IResultDetails? details = null)
    {
        Code = code;
        InnerResult = innerResult;
        Details = details;
        IsSuccess = isSuccess;
        Status = isSuccess ? ResultStatuses.ByName("Success") : ResultStatuses.ByName("Failure");

        var formattedMessage = code.FormatMessage(details);
        var messages = EnsureMessages();
        messages.Add(new GenericMessage(
            isSuccess ? MessageSeverity.Information : MessageSeverity.Error,
            formattedMessage,
            code.Code,
            code.Domain));

        // Copy messages from inner result for visibility
        if (innerResult.Messages.Count > 0)
        {
            messages.AddRange(innerResult.Messages);
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
            EnsureMessages().AddRange(source.Messages);
        }

        RequireMessageOnFailure();
    }

    /// <summary>
    /// Constructor that copies an existing result and adds details to it (FDW-4279). Code, InnerResult, Messages and the outcome carry over; the details are the
    /// source's with the given ones merged over them.
    /// </summary>
    /// <param name="source">The source result to copy.</param>
    /// <param name="details">The details to add; a key the source already holds takes this value.</param>
    protected GenericResult(IGenericResult source, IResultDetails details)
        : this(source)
    {
        Details = MergeDetails(source.Details, details);
    }

    /// <summary>
    /// Constructor that copies a failure whole and adds a message after its own (FDW-4851). Code, InnerResult, Details, status and
    /// the outcome carry over.
    /// </summary>
    /// <param name="source">The failure to copy.</param>
    /// <param name="appended">The message added after the source's messages.</param>
    protected GenericResult(IGenericResult source, IGenericMessage appended)
        : this(source)
    {
        EnsureMessages().Add(appended);
    }

    // The source's details with the added ones over them, in a new instance; the source's own details are read, never changed.
    private static IResultDetails MergeDetails(IResultDetails? existing, IResultDetails added)
    {
        if (existing is null)
        {
            return added;
        }

        var merged = ResultDetails.Create();
        foreach (var pair in existing.Data)
        {
            _ = merged.With(pair.Key, pair.Value);
        }

        foreach (var pair in added.Data)
        {
            _ = merged.With(pair.Key, pair.Value);
        }

        return merged;
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
    public virtual bool IsEmpty => _messages is not { Count: > 0 };

    /// <summary>
    /// Provides collection of messages associated with the result
    /// </summary>
    public IReadOnlyList<IGenericMessage> Messages => _messages is { Count: > 0 } ? _messages.AsReadOnly() : Array.Empty<IGenericMessage>();

    /// <summary>
    /// Gets the most recent message (LIFO - Last In, First Out)
    /// </summary>
    public string? CurrentMessage => _messages?.LastOrDefault()?.Message;


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
        EnsureMessages().Add(message);
    }

    /// <summary>
    /// Adds multiple messages to this result's message collection.
    /// </summary>
    /// <param name="messages">The messages to add.</param>
    /// <ExcludedFromCoverage>Protected extension point for derived classes - not currently used</ExcludedFromCoverage>
    [ExcludeFromCodeCoverage]
    protected void AddMessages(IEnumerable<IGenericMessage> messages)
    {
        AppendMessages(messages);
    }

    private List<IGenericMessage> EnsureMessages() => _messages ??= [];

    // A failed result always says why: every reader takes FailureReason() from it, so a failure built without a
    // message carries the named UnstatedFailure, added here where it is made, instead of each reader inventing one.
    // Not a throw: a result factory runs in endpoints and host phases, which must never throw (Mike, ee99604a, FDW-3580).
    private void RequireMessageOnFailure()
    {
        if (!IsSuccess && _messages is not { Count: > 0 })
            EnsureMessages().Add(UnstatedFailure.Message());
    }

    // An empty collection adds nothing, so it must not be the thing that allocates the list.
    private void AppendMessages(IEnumerable<IGenericMessage> messages)
    {
        if (messages is ICollection<IGenericMessage> { Count: 0 })
            return;

        EnsureMessages().AddRange(messages);
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
    public static IGenericResult Failure(IResultCode code, IResultDetails? details = null) => new GenericResult(code, false, details);

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
        return new GenericResult(code, false, details);
    }

    /// <summary>
    /// Creates a failed result that chains to an inner result, adding context.
    /// </summary>
    /// <param name="code">The result code for the outer failure.</param>
    /// <param name="innerResult">The inner result to wrap.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result with chain.</returns>
    public static IGenericResult Chain(IResultCode code, IGenericResult innerResult, IResultDetails? details = null) =>
        new GenericResult(code, innerResult, false, details);

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
        return new GenericResult(code, innerResult, false, details);
    }

    /// <summary>
    /// Creates the failure of a caller that received a failure: the cause whole, with the caller's own message after its messages
    /// (FDW-4851). The cause's Code, InnerResult, Details and messages carry over, so what classifies the failure (its code, then
    /// its first coded message) is still the cause's; the caller's message says where the failure surfaced and classifies it only
    /// when the cause carries no code.
    /// </summary>
    /// <remarks>
    /// Why not <c>Failure(cause.Messages.Append(context))</c>: that keeps the messages and drops the Code, the chain and the
    /// details (FDW015). Why not <c>Chain</c>: it puts the caller's result code in front, and that code would choose the status.
    /// </remarks>
    /// <param name="cause">The failure the caller received; a success is refused.</param>
    /// <param name="context">The caller's message, which says where the failure surfaced.</param>
    /// <returns>A failed result that is the cause with the caller's message last.</returns>
    public static IGenericResult Caused(IGenericResult cause, IGenericMessage context)
    {
        if (cause.IsSuccess)
        {
            throw new ArgumentException("A cause is a failure; a success has nothing to carry.", nameof(cause));
        }

        return new GenericResult(cause, context);
    }

    /// <summary>
    /// Copies a result and adds details to it, keeping its Code, CodeChain, InnerResult and Messages (FDW-4279). A result is immutable, so this is how a caller
    /// that knows more about a failure than the code that raised it (which connection it came from) states it without rebuilding the failure.
    /// </summary>
    /// <param name="source">The result to copy.</param>
    /// <param name="details">The details to add; a key the source already holds takes this value.</param>
    /// <returns>A result with the source's outcome, code, messages and chain, and its details with the given ones merged over them.</returns>
    public static IGenericResult WithDetails(IGenericResult source, IResultDetails details)
    {
        return new GenericResult(source, details);
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
        // A success reaching here is a real state, not a misuse of the API. Every caller writes
        // `x.IsSuccess && x.Value is not null ? Success(x.Value) : x.ToNewResult<TNew>()`, meaning
        // "propagate the failure" -- and a read that SUCCEEDS and finds nothing takes the else
        // branch. Throwing there reported the caller's syntax while saying nothing about the event,
        // which is a provider returning success with no value; and because several of those callers
        // sit inside endpoint construction and DI factories, the exception escaped as a 500 or took
        // endpoint mapping down with it. A search found 37 sites able to reach this.
        //
        // So it converts rather than throws: the absent value is reported as a failure naming both
        // types. Nothing is invented -- there is no value to invent -- and the absence stays loud,
        // which is the whole point of not returning a bare default here.
        if (IsSuccess)
        {
            return GenericResult<TNew>.Failure(new GenericMessage(
                $"A successful {GetType().Name} carried no value, so it cannot be converted to "
                + $"{typeof(TNew).Name}. The operation reported success and returned nothing."));
        }

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

    private GenericResult(TResult value, IResultCode code, IResultDetails? details = null)
        : this(value, code, SeveritySucceeded(code), details)
    {
    }

    private GenericResult(TResult value, IResultCode code, bool isSuccess, IResultDetails? details = null)
        : base(code, isSuccess, details)
    {
        _value = value;
        _hasValue = isSuccess;
    }

    private GenericResult(TResult value, IResultCode code, IGenericResult innerResult, bool isSuccess, IResultDetails? details = null)
        : base(code, innerResult, isSuccess, details)
    {
        _value = value;
        _hasValue = isSuccess;
    }

    private GenericResult(TResult value, IGenericResult source) : base(source)
    {
        _value = value;
        _hasValue = source.IsSuccess;
    }

    private GenericResult(IGenericResult source, IResultDetails details) : base(source, details)
    {
        _value = default!;
        _hasValue = false;
    }

    /// <summary>
    /// Copies a failed result into a typed one and adds details to it, keeping its Code, CodeChain, InnerResult and Messages (FDW-4279). The value is
    /// <c>default</c>; use this for a failure, never for a success whose value matters.
    /// </summary>
    /// <param name="source">The failed result to copy.</param>
    /// <param name="details">The details to add; a key the source already holds takes this value.</param>
    /// <returns>A typed failure with the source's code, messages and chain, and its details with the given ones merged over them.</returns>
    public new static IGenericResult<TResult> WithDetails(IGenericResult source, IResultDetails details)
    {
        return new GenericResult<TResult>(source, details);
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
    public new static IGenericResult<TResult> Failure(IResultCode code, IResultDetails? details = null) => new GenericResult<TResult>(default!, code, false, details);

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
        return new GenericResult<TResult>(default!, code, false, details);
    }

    /// <summary>
    /// Creates a failed result that chains to an inner result, adding context.
    /// </summary>
    /// <param name="code">The result code for the outer failure.</param>
    /// <param name="innerResult">The inner result to wrap.</param>
    /// <param name="details">Optional details to format into the message.</param>
    /// <returns>A failed result with chain.</returns>
    public new static IGenericResult<TResult> Chain(IResultCode code, IGenericResult innerResult, IResultDetails? details = null) =>
        new GenericResult<TResult>(default!, code, innerResult, false, details);

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
        return new GenericResult<TResult>(default!, code, innerResult, false, details);
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
    /// <param name="failure">Function invoked with the failure's message when the result is a failure; a failure always carries one.</param>
    /// <returns>The value produced by whichever branch was selected.</returns>
    public T Match<T>(Func<TResult, T> success, Func<string, T> failure)
    {
        return IsSuccess ? success(Value) : failure(this.FailureReason().Message);
    }
}
