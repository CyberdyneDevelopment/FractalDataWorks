namespace Fdw.UI.Components.Models;

/// <summary>
/// Client-side DTO for error responses from the API.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>Gets or sets the error code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the reference identifier for correlation.</summary>
    public string? ReferenceId { get; set; }

    /// <summary>Gets or sets whether the operation can be retried.</summary>
    public bool IsRetryable { get; set; }

    /// <summary>Gets or sets the suggested action.</summary>
    public string? Action { get; set; }

    /// <summary>Gets or sets the action URL.</summary>
    public string? ActionUrl { get; set; }
}
