using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Deletes a notification by name: <c>DELETE /notifications/{Name}</c>, 204 when removed and 404 when there is no such notification.
/// </summary>
/// <remarks>
/// It needs the <c>notifications:delete</c> permission, as every resource's delete needs its own.
/// </remarks>
public abstract class DeleteNotificationEndpointBase : CrudDeleteEndpointBase<NotificationNameRequest>
{
    private readonly INotificationConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="DeleteNotificationEndpointBase"/> class.</summary>
    /// <param name="logger">The logger for the endpoint.</param>
    /// <param name="provider">The notification domain's configuration provider.</param>
    protected DeleteNotificationEndpointBase(ILogger<DeleteNotificationEndpointBase> logger, INotificationConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(NotificationNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(NotificationNameRequest request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existing.IsSuccess && existing.Value != null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(NotificationNameRequest request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (!existing.IsSuccess || existing.Value is null)
        {
            return GenericResult.Failure(EndpointLogger.ResourceNotFound(Logger, "Notification", request.Name));
        }

        return await _provider.Delete(existing.Value.Id, ct).ConfigureAwait(false);
    }
}
