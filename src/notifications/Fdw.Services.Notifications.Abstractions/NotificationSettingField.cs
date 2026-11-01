namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// A setting a notification type needs beyond the ones every notification has (FDW-2931).
/// </summary>
/// <param name="Name">The name of the setting, which is the name of the property of the type's configuration that holds it.</param>
/// <param name="DataType">What the value reads as: <see cref="NotificationSettingTypes.Text"/>, <see cref="NotificationSettingTypes.Number"/> or <see cref="NotificationSettingTypes.Flag"/>.</param>
/// <param name="IsRequired">Whether a notification of the type cannot be made without it.</param>
/// <param name="IsSecret">Whether the setting is a secret: it is named (secret manager and key) and never sent as a value.</param>
public sealed record NotificationSettingField(string Name, string DataType, bool IsRequired, bool IsSecret);
