using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;

namespace Fdw.Services.Messaging;

/// <summary>
/// ServiceTypeCollection for messaging service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(MessagingServiceTypeBase),
    typeof(IMessagingServiceType),
    typeof(MessagingServiceTypes),
    ServiceCategory = "Messaging")]
public partial class MessagingServiceTypes : ServiceTypeCollectionBase<
    MessagingServiceTypeBase,
    IMessagingServiceType>
{
}
