using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Webhooks;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Deletes a webhook trigger (FDW-1884). Its address stops answering at once and its name can be used again.</summary>
public abstract class DeleteWebhookEndpointBase : CrudDeleteEndpointBase<WebhookNameRequest>
{
    private readonly WebhookTriggerConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="DeleteWebhookEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored webhook triggers.</param>
    protected DeleteWebhookEndpointBase(ILogger<DeleteWebhookEndpointBase> logger, WebhookTriggerConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "webhooks";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(WebhookNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(WebhookNameRequest request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existing.IsSuccess && existing.Value != null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(WebhookNameRequest request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return existing.IsSuccess && existing.Value is { } found
            ? await _provider.Delete(found.Id, ct).ConfigureAwait(false)
            : GenericResult.Success();
    }
}
