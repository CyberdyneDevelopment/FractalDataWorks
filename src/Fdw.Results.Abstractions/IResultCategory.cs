using Fdw.Collections;

namespace Fdw.Results.Abstractions;

/// <summary>
/// Handling category for a result code — the coarse "what kind of failure, and how is it handled"
/// bucket that the numeric Id range encodes. A closed, framework-owned vocabulary; behavior lives
/// on the option (never a magic-number range tested at a call site).
/// </summary>
public interface IResultCategory : ITypeOption<int, ResultCategoryBase>
{
    /// <summary>
    /// Gets the first Id in this category's 10,000-wide band (equals <c>Id * 10000</c>).
    /// </summary>
    int RangeBase { get; }

    /// <summary>
    /// Gets whether codes in this category represent a failure (false only for the non-error band).
    /// </summary>
    bool IsFailure { get; }

    /// <summary>
    /// Gets whether codes in this category are safe to retry.
    /// </summary>
    bool IsRetryable { get; }

    /// <summary>
    /// Gets the suggested HTTP status for codes in this category (a hint for the HTTP mapper).
    /// </summary>
    int HttpStatus { get; }

    /// <summary>
    /// Gets a client-safe, generic message for codes in this category. Never echoes raw failure
    /// detail (SQL, host, credentials); the HTTP mapper returns this instead of the result message.
    /// </summary>
    string ClientMessage { get; }

    /// <summary>
    /// Gets a suggested client remediation action for this category, or <c>null</c> when none applies.
    /// </summary>
    string? ClientAction { get; }
}
