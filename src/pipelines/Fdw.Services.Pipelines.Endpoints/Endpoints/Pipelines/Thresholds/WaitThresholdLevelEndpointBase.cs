using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// What the routes of the level of thresholds on waiting runs share (FDW-3161): the owner the route is for, the store, and how a result is answered: the level when the store
/// answers one, otherwise the status its failure names (a refusal 400, an owner that does not exist 404, a fault 500).
/// </summary>
/// <typeparam name="TRequest">The request: none for a read, the level for a write.</typeparam>
public abstract class WaitThresholdLevelEndpointBase<TRequest> : Endpoint<TRequest, WaitThresholdLevelPayload>
    where TRequest : notnull
{
    /// <summary>Initializes a new instance of the <see cref="WaitThresholdLevelEndpointBase{TRequest}"/> class.</summary>
    /// <param name="store">Reads and writes the level.</param>
    protected WaitThresholdLevelEndpointBase(IWaitThresholdStore store)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>Gets the kind of owner the route is for, as <c>WaitThresholdOwners</c> names it.</summary>
    protected abstract string OwnerKind { get; }

    /// <summary>Gets the route, with <c>{name}</c> for the pipeline or schedule when the owner is one.</summary>
    protected abstract string RoutePath { get; }

    /// <summary>Gets the store.</summary>
    protected IWaitThresholdStore Store { get; }

    /// <summary>Gets the name of the pipeline or schedule the route names; none for the server.</summary>
    /// <returns>The name.</returns>
    protected string? OwnerName() => string.Equals(OwnerKind, WaitThresholdOwners.Server, StringComparison.Ordinal) ? null : Route<string>("name");

    /// <summary>Answers what the store answered.</summary>
    /// <param name="result">The store's answer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that ends when the answer is sent.</returns>
    protected async Task Answer(IGenericResult<WaitThresholdLevelPayload> result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsSuccess && result.Value is { } level)
        {
            await Send.OkAsync(level, cancellationToken).ConfigureAwait(false);
            return;
        }

        var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
    }
}
