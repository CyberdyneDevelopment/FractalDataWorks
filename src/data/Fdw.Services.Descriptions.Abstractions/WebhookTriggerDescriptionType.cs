using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A webhook trigger's description: what calling it starts, in the words of whoever made it (FDW-7323). Read by whoever holds <c>webhooks:read</c>, the permission the webhook routes ask. The trigger is the thing described, so the type has no owner: a trigger is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class WebhookTriggerDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the webhook trigger provider writes and reads its descriptions under.</summary>
    public const string OptionName = "WebhookTrigger";

    /// <summary>Initializes a new instance of the <see cref="WebhookTriggerDescriptionType"/> class.</summary>
    public WebhookTriggerDescriptionType()
        : base(OptionName, "webhooks:read", hasOwner: false)
    {
    }
}
