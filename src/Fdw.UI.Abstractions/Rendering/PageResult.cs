using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Rendering;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// Result of a page render operation.
/// </summary>
public sealed class PageResult
{
    private PageResult(
        bool success,
        IPageActionType action,
        object? savedConfiguration,
        ValidationResult? validation,
        string? error)
    {
        Success = success;
        Action = action;
        SavedConfiguration = savedConfiguration;
        Validation = validation;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the page operation succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the action taken by the user.
    /// </summary>
    public IPageActionType Action { get; }

    /// <summary>
    /// Gets the saved configuration if the user saved.
    /// </summary>
    public object? SavedConfiguration { get; }

    /// <summary>
    /// Gets the validation result if validation failed.
    /// </summary>
    public ValidationResult? Validation { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful save result.
    /// </summary>
    /// <param name="configuration">The saved configuration.</param>
    /// <returns>A save result.</returns>
    public static PageResult Save(object configuration) =>
        new(true, PageActions.Save, configuration, null, null);

    /// <summary>
    /// Creates a cancel result.
    /// </summary>
    /// <returns>A cancel result.</returns>
    public static PageResult Cancel() =>
        new(true, PageActions.Cancel, null, null, null);

    /// <summary>
    /// Creates a delete result.
    /// </summary>
    /// <returns>A delete result.</returns>
    public static PageResult Delete() =>
        new(true, PageActions.Delete, null, null, null);

    /// <summary>
    /// Creates a validation failure result.
    /// </summary>
    /// <param name="validation">The validation result.</param>
    /// <returns>A validation failure result.</returns>
    public static PageResult ValidationFailed(ValidationResult validation) =>
        new(false, PageActions.None, null, validation, null);

    /// <summary>
    /// Creates an error result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>An error result.</returns>
    public static PageResult Failure(string error) =>
        new(false, PageActions.None, null, null, error);
}
