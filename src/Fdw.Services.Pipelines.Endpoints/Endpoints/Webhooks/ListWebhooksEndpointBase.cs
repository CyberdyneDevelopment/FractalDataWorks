using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Fdw.Services.Pipelines.Webhooks;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Lists the webhook triggers, a page at a time (FDW-1884). A row never carries the secret or its hash.</summary>
public abstract class ListWebhooksEndpointBase : CrudPageEndpointBase<DomainConfiguration, WebhookSummaryPayload>
{
    private readonly WebhookTriggerConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="ListWebhooksEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored webhook triggers.</param>
    protected ListWebhooksEndpointBase(ILogger<ListWebhooksEndpointBase> logger, WebhookTriggerConfigurationProvider provider) : base(logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    protected override string ResourceName => "webhooks";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> LoadPage(PaginatedListRequest paging, CancellationToken ct) =>
        _provider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct);

    /// <inheritdoc />
    protected override ValueTask<bool> Include(DomainConfiguration item, CancellationToken ct) =>
        ValueTask.FromResult(!string.IsNullOrWhiteSpace(item.Name));

    /// <inheritdoc />
    protected override async Task<IGenericResult<WebhookSummaryPayload>> ToSummary(DomainConfiguration item, CancellationToken ct)
    {
        var read = await _provider.Get(item.Name, ct).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<WebhookSummaryPayload>();
        }

        return read.Value is WebhookTriggerConfiguration trigger
            ? GenericResult<WebhookSummaryPayload>.Success(WebhookPayloadWriter.Summarize(trigger))
            : GenericResult<WebhookSummaryPayload>.Failure(WebhookEndpointLog.SavedButUnreadable(Logger, item.Name));
    }
}
