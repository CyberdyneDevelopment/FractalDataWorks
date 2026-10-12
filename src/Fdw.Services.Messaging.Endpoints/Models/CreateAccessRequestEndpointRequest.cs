namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Request for creating an access request.
/// </summary>
public class CreateAccessRequestEndpointRequest
{
    /// <summary>
    /// Gets or sets the resource being requested.
    /// </summary>
    public string RequestedResource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission being requested.
    /// </summary>
    public string RequestedPermission { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the justification for the request.
    /// </summary>
    public string? Justification { get; set; }

    /// <summary>
    /// Gets or sets a reference identifier for correlation.
    /// </summary>
    public string? ReferenceId { get; set; }
}
