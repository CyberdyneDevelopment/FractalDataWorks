using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Request DTO that identifies a notification by name.
/// </summary>
public sealed class NotificationNameRequest
{
    /// <summary>Gets or sets the notification name: the <c>name</c> field of the body of a read, like every other read of one record (FDW-1013).</summary>
    [Required]
    public string Name { get; set; } = string.Empty;
}
