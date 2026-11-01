using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Endpoints;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Reads the notification lists kept in the configuration store (FDW-2931).
/// </summary>
public interface INotificationLists
{
    /// <summary>Lists the current notification lists in name order, each with the number of enabled members it holds.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The lists, none when the store holds none; a failure when the store cannot be read.</returns>
    Task<IGenericResult<IReadOnlyList<NotificationListSummaryDto>>> List(CancellationToken cancellationToken = default);
}
