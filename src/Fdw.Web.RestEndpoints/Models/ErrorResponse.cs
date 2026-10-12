namespace Fdw.Web.RestEndpoints.Models;

/// <summary>
/// Structured error response returned to API clients.
/// Contains only user-safe information — no server addresses, SQL text, or stack traces.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error code — the result code's <c>{prefix}-{number}</c> identifier, e.g. <c>MESSAGING-91000</c>.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user-safe error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation/reference ID for support lookup.
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation can be retried.
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// Gets or sets the suggested user action (e.g., "Contact your administrator").
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the URL for the suggested action (e.g., "/access-requests/new").
    /// </summary>
    public string? ActionUrl { get; set; }
}
