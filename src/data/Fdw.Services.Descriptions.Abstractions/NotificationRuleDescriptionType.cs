using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A notification rule's description: what the rule is for, in the words of whoever made it (FDW-7203). Read by whoever holds <c>notifications/rules:read</c>, the permission the notification rule routes ask. The rule is the thing described, so the type has no owner: a rule is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class NotificationRuleDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the notification rule provider writes and reads its descriptions under.</summary>
    public const string OptionName = "NotificationRule";

    /// <summary>Initializes a new instance of the <see cref="NotificationRuleDescriptionType"/> class.</summary>
    public NotificationRuleDescriptionType()
        : base(OptionName, "notifications/rules:read", hasOwner: false)
    {
    }
}
