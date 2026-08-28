using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Services.Data.Abstractions;
// FindEndpointLog is in this namespace
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.Search.Clients.Models;

namespace Fdw.Web.Search.Endpoints;

/// <summary>
/// Endpoint for cross-field find (search within a container's string fields).
/// </summary>
public class FindEndpoint : Endpoint<FindRequest, FindResponse>
{
    private readonly IDataGateway _dataGateway;
    private readonly ILogger<FindEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FindEndpoint"/> class.
    /// </summary>
    public FindEndpoint(
        IDataGateway dataGateway,
        ILogger<FindEndpoint>? logger = null)
    {
        _dataGateway = dataGateway;
        _logger = logger ?? NullLogger<FindEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/find");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datastores:read");
#endif
        Summary(s =>
        {
            s.Summary = "Cross-field find";
            s.Description = "Searches within a container's string fields using LIKE matching. Returns matched records with which fields matched.";
            s.ExampleRequest = new FindRequest
            {
                DataStoreName = "PlatformConfiguration",
                PathName = "data",
                ContainerName = "Connection",
                SearchTerm = "Nfl",
                MaxResults = 50
            };
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(FindRequest req, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        var maxResults = Math.Min(Math.Max(req.MaxResults, 1), 500);

        FindEndpointLog.FindRequestReceived(_logger, req.DataStoreName, req.PathName, req.ContainerName, maxResults);
        FindEndpointLog.FindStarted(_logger, req.SearchTerm, req.ContainerName, req.FieldNames?.Count ?? 0);

        var command = new FindCommand<Dictionary<string, object?>>
        {
            SearchTerm = req.SearchTerm,
            FieldNames = req.FieldNames?.ToList(),
            CaseSensitive = req.CaseSensitive,
            MaxResults = maxResults
        };

        FindEndpointLog.DispatchingFindCommand(_logger, req.ContainerName);
        var result = await _dataGateway.Execute<IEnumerable<FindResult<Dictionary<string, object?>>>>(
                command, new DataStoreTarget(req.DataStoreName, req.PathName, req.ContainerName), ct)
            .ConfigureAwait(false);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        if (!result.IsSuccess || result.Value == null)
        {
            var upstreamError = result.CurrentMessage;
            if (upstreamError is not null)
            {
                FindEndpointLog.FindFailed(_logger, null!, upstreamError);
            }
            else
            {
                FindEndpointLog.FindFailedNoDetails(_logger, req.ContainerName);
            }
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var items = result.Value.Select(r => new FindResultPayload
        {
            Record = r.Record,
            MatchedFields = r.MatchedFields.ToList()
        }).ToList();

        if (items.Count == 0)
        {
            FindEndpointLog.FindNoResults(_logger, req.SearchTerm, req.ContainerName);
        }
        else
        {
            FindEndpointLog.FindCompleted(_logger, items.Count, elapsed);
        }

        var response = new FindResponse
        {
            SearchTerm = req.SearchTerm,
            ContainerName = req.ContainerName,
            TotalCount = items.Count,
            Results = items,
            DurationMs = elapsed
        };

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }
}
