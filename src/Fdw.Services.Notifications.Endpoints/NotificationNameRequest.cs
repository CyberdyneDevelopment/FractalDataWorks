using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Request DTO that identifies a notification by name.
/// </summary>
public sealed class NotificationNameRequest
{
    /// <summary>Gets or sets the notification name (from route).</summary>
    [Required]
    public string NotificationName { get; set; } = string.Empty;
}
