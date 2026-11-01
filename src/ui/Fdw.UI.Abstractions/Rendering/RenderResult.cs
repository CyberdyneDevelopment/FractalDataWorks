using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// Result of a render operation.
/// </summary>
public sealed class RenderResult
{
    private RenderResult(bool success, string? error = null)
    {
        Success = success;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the render succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the error message if rendering failed.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful render result.
    /// </summary>
    /// <returns>A successful render result.</returns>
    public static RenderResult Ok() => new(true);

    /// <summary>
    /// Creates a failed render result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed render result.</returns>
    public static RenderResult Failure(string error) => new(false, error);
}