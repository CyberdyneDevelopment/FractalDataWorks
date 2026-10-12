using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
// FindEndpointLog is in this namespace
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.Search.Clients.Models;

namespace Fdw.Web.Search.Endpoints;

/// <summary>
/// Endpoint for cross-field find (search within a container's string fields).
/// </summary>
public class FindEndpoint : Endpoint<FindRequest, FindResponse>
{
    private readonly IDataGatewayProvider _dataGateways;

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");
    private readonly ILogger<FindEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FindEndpoint"/> class.
    /// </summary>
    public FindEndpoint(
        IDataGatewayProvider dataGateways,
        ILogger<FindEndpoint>? logger = null)
    {
        _dataGateways = dataGateways;
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
                DataStoreName = "NflDb",
                PathName = "NflData",
                ContainerName = "Team",
                SearchTerm = "Pat",
                MaxResults = 50
            };
        });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The response is written exactly once (FDW-1162). A find the gateway refuses or cannot run answers with that
    /// failure's own status through <see cref="ResultHttpStatusMapper"/>: a refusal is a 400 that says why, and only a
    /// target that does not exist is a 404, with a body.
    /// </remarks>
    public override async Task HandleAsync(FindRequest req, CancellationToken ct)
    {
        var found = await Find(req, ct).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { } response)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(found, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>Searches the container the request names and returns its matches.</summary>
    /// <param name="req">The find request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matches, or the gateway's own failure when the target was refused or could not be searched.</returns>
    public async Task<IGenericResult<FindResponse>> Find(FindRequest req, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(req);

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
        var result = await Gateway.Execute<IEnumerable<FindResult<Dictionary<string, object?>>>>(
                command, new DataStoreTarget(req.DataStoreName, req.PathName, req.ContainerName), cancellationToken)
            .ConfigureAwait(false);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Why the gateway's own result travels on: its code decides the status and, for a refusal, the reason the
        // caller is shown. Answering every failure "not found" told a refused caller the container did not exist.
        if (result.IsFailure || result.Value is not { } matches)
        {
            FindEndpointLog.FindNotAnswered(_logger, req.ContainerName);
            return result.ToNewResult<FindResponse>();
        }

        var items = matches.Select(r => new FindResultPayload
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

        return GenericResult<FindResponse>.Success(new FindResponse
        {
            SearchTerm = req.SearchTerm,
            ContainerName = req.ContainerName,
            TotalCount = items.Count,
            Results = items,
            DurationMs = elapsed
        });
    }
}
