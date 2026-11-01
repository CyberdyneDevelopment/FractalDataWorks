using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Services.Notifications.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Notifications;

/// <summary>
/// Reads the notification lists and their members from <c>notify.NotificationList</c> and <c>notify.NotificationListMember</c> (FDW-2931).
/// </summary>
public sealed class NotificationLists : INotificationLists
{
    private const string PathName = "notify";
    private const string ListTable = "NotificationList";
    private const string MemberTable = "NotificationListMember";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly string _dataStoreName;
    private readonly ILogger<NotificationLists> _logger;

    /// <summary>Initializes a new instance of the <see cref="NotificationLists"/> class.</summary>
    /// <param name="gateways">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection the lists are kept on.</param>
    /// <param name="logger">The logger.</param>
    public NotificationLists(IConfigurationGatewayProvider gateways, string dataStoreName, ILogger<NotificationLists>? logger)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _dataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        _logger = logger ?? NullLogger<NotificationLists>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<NotificationListSummaryDto>>> List(CancellationToken cancellationToken = default)
    {
        var gateway = _gateways.Get(_dataStoreName);
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return GenericResult<IReadOnlyList<NotificationListSummaryDto>>.Failure(
                NotificationListLog.ReadFailed(_logger, "The configuration store", gateway.CurrentMessage));
        }

        var lists = await Current<NotificationListConfiguration>(resolved, ListTable, cancellationToken).ConfigureAwait(false);
        if (lists.IsFailure || lists.Value is not { } foundLists)
        {
            return lists.ToNewResult<IReadOnlyList<NotificationListSummaryDto>>();
        }

        var members = await Current<NotificationListMemberConfiguration>(resolved, MemberTable, cancellationToken).ConfigureAwait(false);
        if (members.IsFailure || members.Value is not { } foundMembers)
        {
            return members.ToNewResult<IReadOnlyList<NotificationListSummaryDto>>();
        }

        var enabledPerList = foundMembers.Where(member => member.IsEnabled).GroupBy(member => member.NotificationListId).ToDictionary(group => group.Key, group => group.Count());
        var summaries = foundLists
            .OrderBy(list => list.Name, StringComparer.Ordinal)
            .Select(list => new NotificationListSummaryDto
            {
                Id = list.Id, Name = list.Name, Description = list.Description, IsEnabled = list.IsEnabled,
                MemberCount = enabledPerList.TryGetValue(list.Id, out var count) ? count : 0,
            })
            .ToList();

        NotificationListLog.ListsRead(_logger, summaries.Count, foundMembers.Count);
        return GenericResult<IReadOnlyList<NotificationListSummaryDto>>.Success(summaries);
    }

    // The current, not-deleted rows of one table, or the failure to read them.
    private async Task<IGenericResult<IReadOnlyList<TRow>>> Current<TRow>(
        IConfigurationGateway gateway, string table, CancellationToken cancellationToken)
    {
        var read = await gateway.Execute<IEnumerable<TRow>>(
            Query.From<TRow>(_dataStoreName, PathName, table).Where("IsCurrent", true).Where("IsDeleted", false).Build(),
            cancellationToken).ConfigureAwait(false);
        return read.IsSuccess && read.Value is { } rows
            ? GenericResult<IReadOnlyList<TRow>>.Success([.. rows])
            : GenericResult<IReadOnlyList<TRow>>.Failure(NotificationListLog.ReadFailed(_logger, table, read.CurrentMessage));
    }
}
