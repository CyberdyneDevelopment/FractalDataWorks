using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A notification list's description: who the list of recipients is for, in the words of whoever made it (FDW-7203). Read by whoever holds <c>notifications/lists:read</c>, the permission the notification list route asks. The list is the thing described, so the type has no owner: a list is read with its own description and nothing in FDW removes one.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class NotificationListDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the notification lists reader writes and reads its descriptions under.</summary>
    public const string OptionName = "NotificationList";

    /// <summary>Initializes a new instance of the <see cref="NotificationListDescriptionType"/> class.</summary>
    public NotificationListDescriptionType()
        : base(OptionName, "notifications/lists:read", hasOwner: false)
    {
    }
}
