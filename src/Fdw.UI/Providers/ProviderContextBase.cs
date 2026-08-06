using System;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.UI.Providers.Results;

namespace Fdw.UI.Providers;

/// <summary>
/// Base for the context a headless provider hands to its <c>ChildContent</c> render fragment.
/// </summary>
/// <remarks>
/// <para>
/// Why this is a base type rather than a convention: every provider context declared
/// <c>IsLoading</c> and <c>ErrorMessage</c> independently, so nothing reconciled two contexts that
/// disagreed — and they did disagree. Two of them omitted <c>IsLoading</c> entirely, refresh was
/// spelled both <c>OnRefresh</c> and <c>OnReload</c>, and no context carried the
/// <see cref="IGenericResult"/> its API client had already returned. A context now inherits the
/// shape once, so disagreement is not expressible. This is the same move
/// <c>NavSectionBase</c> made for sidebar sections.
/// </para>
/// <para>
/// Why <see cref="LastResult"/> and not a string: the clients return
/// <c>IGenericResult&lt;T&gt;</c> carrying a ResultCode, a message chain and a root cause.
/// Providers were unwrapping that to <c>null</c> and substituting a locally re-derived string, so
/// the page could not tell "not found" from "not authorized" from "the request never left". The
/// envelope now reaches the page intact; <see cref="ErrorMessage"/> is a projection of it, not a
/// second, divergent copy of the truth.
/// </para>
/// </remarks>
public abstract class ProviderContextBase
{
    /// <summary>
    /// Gets a value indicating whether an operation is in flight.
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// Gets the result of the most recent operation this provider ran, or <see langword="null"/>
    /// before the first one completes.
    /// </summary>
    public IGenericResult? LastResult { get; init; }

    /// <summary>
    /// Gets the failure message from <see cref="LastResult"/>, or <see langword="null"/> when the
    /// last operation succeeded, was cancelled, or none has run.
    /// </summary>
    public string? ErrorMessage => HasError ? LastResult!.CurrentMessage : null;

    /// <summary>
    /// Gets a value indicating whether the last operation failed in a way worth showing the user.
    /// </summary>
    /// <remarks>
    /// Cancellation is excluded: a request abandoned because the component was disposed or the user
    /// navigated away is not something to paint an error banner for. This is the one place that
    /// judgement is made, so every context agrees on it.
    /// </remarks>
    public bool HasError =>
        LastResult is { IsFailure: true } result &&
        !string.Equals(result.Code?.Name, "OperationCancelled", StringComparison.Ordinal);

    /// <summary>
    /// Gets the callback that re-runs this provider's load.
    /// </summary>
    /// <remarks>
    /// Defaults to a <see cref="UIProviderResultCodes"/> failure rather than a completed task so a
    /// provider that forgot to wire it fails loud at the call site instead of reporting success for
    /// an operation that never ran. No <c>CancellationToken</c> parameter: these are bound directly
    /// to Blazor event handlers, the documented exemption to the propagation rule — the provider
    /// owns the token and passes it to the client on the other side of the callback.
    /// </remarks>
    public Func<Task<IGenericResult>> OnRefresh { get; init; } = CallbackNotProvided;

    /// <summary>
    /// Produces the failure a context callback returns when its provider never supplied it.
    /// </summary>
    /// <returns>A failure result carrying the CallbackNotProvided code.</returns>
    public static Task<IGenericResult> CallbackNotProvided() =>
        Task.FromResult(GenericResult.Failure(UIProviderResultCodes.ByName("CallbackNotProvided")));

    /// <summary>
    /// Produces the typed failure a context callback returns when its provider never supplied it.
    /// </summary>
    /// <typeparam name="T">The value type the callback would have produced.</typeparam>
    /// <returns>A typed failure result carrying the CallbackNotProvided code.</returns>
    public static Task<IGenericResult<T>> CallbackNotProvided<T>() =>
        Task.FromResult(GenericResult<T>.Failure(UIProviderResultCodes.ByName("CallbackNotProvided")));

    /// <summary>
    /// Produces the result a provider operation returns when it was cancelled mid-flight.
    /// </summary>
    /// <remarks>
    /// A failure, not a success: the operation did not complete and produced no value, and the
    /// typed overload cannot manufacture one. Both overloads therefore agree. Cancellation stays
    /// out of the user's face because <see cref="ErrorMessage"/> and <see cref="HasError"/> filter
    /// this code, not because the two overloads disagree about what happened.
    /// </remarks>
    /// <returns>A failure result carrying the OperationCancelled code.</returns>
    public static IGenericResult Cancelled() =>
        GenericResult.Failure(UIProviderResultCodes.ByName("OperationCancelled"));

    /// <summary>
    /// Produces the typed result a provider operation returns when it was cancelled mid-flight.
    /// </summary>
    /// <typeparam name="T">The value type the operation would have produced.</typeparam>
    /// <returns>A non-error typed result carrying the OperationCancelled code.</returns>
    public static IGenericResult<T> Cancelled<T>() =>
        GenericResult<T>.Failure(UIProviderResultCodes.ByName("OperationCancelled"));
}
