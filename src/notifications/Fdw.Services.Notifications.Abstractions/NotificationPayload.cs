using System.Collections.Generic;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// What a notification carries beyond its words (FDW-6138): the payload's type, a name from <see cref="NotificationPayloadTypes"/>, and the values of the fields that type declares, each as the text
/// it was stated with.
/// </summary>
public sealed class NotificationPayload
{
    /// <summary>Initializes a new instance of the <see cref="NotificationPayload"/> class.</summary>
    /// <param name="typeName">The name of the payload type.</param>
    /// <param name="fields">The values of the fields, by name.</param>
    public NotificationPayload(string typeName, IReadOnlyDictionary<string, string> fields)
    {
        TypeName = typeName;
        Fields = fields;
    }

    /// <summary>Gets the name of the payload type.</summary>
    public string TypeName { get; }

    /// <summary>Gets the values of the fields, by name.</summary>
    public IReadOnlyDictionary<string, string> Fields { get; }
}
