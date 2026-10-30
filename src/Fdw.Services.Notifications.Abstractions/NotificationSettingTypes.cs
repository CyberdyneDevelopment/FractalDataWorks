namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// What the value of a notification setting reads as (FDW-2931).
/// </summary>
public static class NotificationSettingTypes
{
    /// <summary>A string.</summary>
    public const string Text = "text";

    /// <summary>A whole number.</summary>
    public const string Number = "number";

    /// <summary>True or false.</summary>
    public const string Flag = "flag";
}
