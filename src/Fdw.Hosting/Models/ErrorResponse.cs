using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Models;

/// <summary>
/// Standard error response for 500 Internal Server Error responses.
/// Includes request ID for support tracking and contact information.
/// </summary>
// Why: pure DTO serialized directly to the HTTP response body, no logic.
[ExcludeFromCodeCoverage]
public sealed class ErrorResponse
{
    public string RequestId { get; set; } = "";

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public int StatusCode { get; set; } = 500;

    public string Message { get; set; } = "An unexpected error occurred.";

    public SupportContactInfo Support { get; set; } = new();
}
