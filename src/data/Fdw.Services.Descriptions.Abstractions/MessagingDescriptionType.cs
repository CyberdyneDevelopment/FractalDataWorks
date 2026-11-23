using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A messaging configuration's description: what the configured messaging is for, in the words of whoever configured it (FDW-7203). Read by whoever holds <c>messages:read</c>, the permission the messaging routes ask. The messaging configuration is the thing described, so the type has no owner: it is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class MessagingDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the messaging provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Messaging";

    /// <summary>Initializes a new instance of the <see cref="MessagingDescriptionType"/> class.</summary>
    public MessagingDescriptionType()
        : base(OptionName, "messages:read", hasOwner: false)
    {
    }
}
