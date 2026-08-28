using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for listing all notification configurations.
/// </summary>
public abstract class ListNotificationsEndpointBase : CrudListEndpointBase<NotificationSummaryDto>
{
    private readonly IServiceConfigurationProvider<NotificationConfiguration> _provider;

    /// <inheritdoc />
    protected ListNotificationsEndpointBase(IServiceConfigurationProvider<NotificationConfiguration> provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string EndpointSummary => "List notifications";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all notification configurations.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<NotificationSummaryDto>>> LoadItems(CancellationToken ct)
    {
        NotificationEndpointLog.ListingNotifications(Logger);

        var allResult = await _provider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            return allResult.ToNewResult<List<NotificationSummaryDto>>();
        }

        var items = (allResult.Value ?? (IReadOnlyList<NotificationConfiguration>)[])
            .Select(n => new NotificationSummaryDto
            {
                Id = n.Id,
                Name = n.Name,
                ServiceOptionType = n.ServiceOptionType,
                Description = n.Description
            })
            .ToList();

        return GenericResult<List<NotificationSummaryDto>>.Success(items);
    }
}
