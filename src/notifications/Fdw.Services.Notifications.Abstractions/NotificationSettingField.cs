namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// A setting a notification type needs beyond the ones every notification has (FDW-2931).
/// </summary>
/// <param name="Name">The name of the setting, which is the name of the property of the type's configuration that holds it.</param>
/// <param name="DataType">What the value reads as: <see cref="NotificationSettingTypes.Text"/>, <see cref="NotificationSettingTypes.Number"/> or <see cref="NotificationSettingTypes.Flag"/>.</param>
/// <param name="IsRequired">Whether a notification of the type cannot be made without it.</param>
/// <param name="IsSecret">Whether the setting is a secret: it is named (secret manager and key) and never sent as a value.</param>
public sealed record NotificationSettingField(string Name, string DataType, bool IsRequired, bool IsSecret)
{
    /// <summary>
    /// Gets the name of the setting that holds this setting's default, which a notification being made without it takes (FDW-3580), or
    /// <see langword="null"/> when it has none. Mike: "I don't care what you set the default to as long as it is in configuration and not hardcoded".
    /// </summary>
    public string? DefaultSetting { get; init; }
}
