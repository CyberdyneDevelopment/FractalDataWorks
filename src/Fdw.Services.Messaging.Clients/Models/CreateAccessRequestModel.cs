namespace Fdw.Services.Messaging.Clients.Models;

/// <summary>
/// Form model for creating an access request.
/// </summary>
public sealed class CreateAccessRequestModel
{
    /// <summary>Gets or sets the resource to which access is being requested.</summary>
    public string RequestedResource { get; set; } = string.Empty;

    /// <summary>Gets or sets the permission being requested on the resource.</summary>
    public string RequestedPermission { get; set; } = string.Empty;

    /// <summary>Gets or sets the requester's justification, if provided.</summary>
    public string? Justification { get; set; }

    /// <summary>Gets or sets an external correlation/reference identifier, if any.</summary>
    public string? ReferenceId { get; set; }
}
