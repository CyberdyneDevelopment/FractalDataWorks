namespace Fdw.Services.Notifications.Abstractions;

/// <summary>A field a payload type declares (FDW-6138).</summary>
/// <param name="Name">The field's name.</param>
/// <param name="IsRequired">Whether a payload of the type must state it.</param>
public sealed record PayloadField(string Name, bool IsRequired);
