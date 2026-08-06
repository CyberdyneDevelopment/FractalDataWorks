namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// Result of a prompt operation.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class PromptResult<T>
{
    private PromptResult(bool success, T? value, bool cancelled, string? error)
    {
        Success = success;
        Value = value;
        Cancelled = cancelled;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the prompt succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the value entered by the user.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets a value indicating whether the user cancelled the prompt.
    /// </summary>
    public bool Cancelled { get; }

    /// <summary>
    /// Gets the error message if prompting failed.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful prompt result.
    /// </summary>
    /// <param name="value">The value entered by the user.</param>
    /// <returns>A successful prompt result.</returns>
    public static PromptResult<T> Ok(T value) => new(true, value, false, null);

    /// <summary>
    /// Creates a cancelled prompt result.
    /// </summary>
    /// <returns>A cancelled prompt result.</returns>
    public static PromptResult<T> Cancel() => new(false, default, true, null);

    /// <summary>
    /// Creates a failed prompt result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed prompt result.</returns>
    public static PromptResult<T> Failure(string error) => new(false, default, false, error);
}