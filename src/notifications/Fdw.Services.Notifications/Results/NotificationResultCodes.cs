using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// TypeCollection for Notification service result codes.
/// EventId range: 6200-6249 (within Services 6000-6999)
/// </summary>
[TypeCollection(typeof(NotificationResultCodeBase), typeof(IResultCode), typeof(NotificationResultCodes))]
public abstract partial class NotificationResultCodes : TypeCollectionBase<NotificationResultCodeBase, IResultCode>
{
}

// =============================================================================
// Notification Validation Result Codes
// =============================================================================

// =============================================================================
// Notification Service Result Codes
// =============================================================================