using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Components.Error;

/// <summary>
/// Context for rendering API result display (success or error).
/// </summary>
// Why: pure DTO, no logic.
[ExcludeFromCodeCoverage]
public sealed class ApiResultContext
{
    /// <summary>
    /// Gets a value indicating whether the result represents success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the success message, if applicable.
    /// </summary>
    public string? SuccessMessage { get; }

    /// <summary>
    /// Gets the error message, if applicable.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the result code, if available.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Gets a value indicating whether the result is loading.
    /// </summary>
    public bool IsLoading { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiResultContext"/> class.
    /// </summary>
    public ApiResultContext(
        bool isSuccess,
        string? successMessage,
        string? errorMessage,
        string? code,
        bool isLoading)
    {
        IsSuccess = isSuccess;
        SuccessMessage = successMessage;
        ErrorMessage = errorMessage;
        Code = code;
        IsLoading = isLoading;
    }
}
