using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A notification's description: what the notification channel is for, in the words of whoever configured it (FDW-7203). Read by whoever holds <c>notifications:read</c>, the permission the notification routes ask. The notification is the thing described, so the type has no owner: a notification is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class NotificationDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the notification provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Notification";

    /// <summary>Initializes a new instance of the <see cref="NotificationDescriptionType"/> class.</summary>
    public NotificationDescriptionType()
        : base(OptionName, "notifications:read", hasOwner: false)
    {
    }
}
