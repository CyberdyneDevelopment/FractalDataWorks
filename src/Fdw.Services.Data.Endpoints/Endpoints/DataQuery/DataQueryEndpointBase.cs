using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Queries one data set: a filter tree, an ordering and a page, answered with the data set's
/// declared fields only.
/// </summary>
/// <typeparam name="TRequest">The body and route values.</typeparam>
/// <typeparam name="TResponse">The page answered.</typeparam>
/// <remarks>
/// <para>
/// The query is the same whoever asks. What differs is which data set a name reaches, so that is
/// the one step a derived route supplies: <c>POST /data/{DataSetName}</c> reaches the data set by
/// name, and a layer over it (a workspace) reaches only the data sets attached to it (FDW-975).
/// </para>
/// <para>
/// The query dispatches through the gateway's <see cref="DataSetTarget"/> path, so Simple, Compound
/// and Federated data sets all work, and it runs under the caller's own session context, so the
/// data store's row-level security narrows the rows for this caller.
/// </para>
/// <para>
/// A caller no page limit applies to (an API key whose ApiKey paging settings are 0, FDW-1209) who names no
/// <c>take</c> is answered with every row, written as <see cref="DataQueryStream"/> reads them, batch by batch, in a
/// <see cref="DataQueryStreamResponse"/> and not a <typeparamref name="TResponse"/>. No ceiling is added beside the
/// settings: an operator who wants one sets the ApiKey largest page, the connection's own result-size and time limits
/// still apply, and a query that filters or sorts on a calculated field is still held to the calculated row cap.
/// A grouped query is read whole, as a page of groups with no take.
/// </para>
/// <para>
/// A streamed response must not pass for a whole one when it is not. A failure on the first batch answers with a real error
/// status, like any other request, because nothing has been written. A failure on a later batch aborts the connection and
/// leaves the JSON unfinished, so a client or parser throws; a response that finished ends in <c>rowCount</c> and
/// <c>complete: true</c>, which no cut-off response carries.
/// </para>
/// </remarks>
public abstract class DataQueryEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : DataQueryRequest
    where TResponse : DataQueryResponse, new()
{
    private readonly DataSetQuery _query;

    /// <summary>Initializes a new instance of the <see cref="DataQueryEndpointBase{TRequest, TResponse}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataGateways">Supplies the gateway the query is dispatched through.</param>
    protected DataQueryEndpointBase(ILogger logger, IDataGatewayProvider dataGateways)
    {
        Logger = logger ?? NullLogger.Instance;
        _query = new DataSetQuery(dataGateways ?? throw new ArgumentNullException(nameof(dataGateways)), Logger);
    }

    /// <summary>Gets the logger.</summary>
    protected new ILogger Logger { get; }

    /// <inheritdoc />
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        EndpointLogger.ExecutingEndpoint(Logger, GetType().Name, typeof(TRequest).Name);

        var prepared = await Prepare(req, ct).ConfigureAwait(false);
        if (prepared.IsFailure)
        {
            await SendFailure(prepared).ConfigureAwait(false);
            return;
        }

        var (dataSet, paging, rowCap) = prepared.Value;

        // Why a grouped query is not streamed: its rows are groups, ordered by the query's own group ordering and not by the
        // data set's keys, so there is nothing to cut batches by; it is read whole in one go. The groups are as many as the
        // data set has distinct values, which is not what a row export is.
        if (paging.IsUnlimited && !DataQueryGrouping.IsRequested(req))
        {
            await Stream(req, dataSet, rowCap, ct).ConfigureAwait(false);
        }
        else
        {
            var response = await Query(req, dataSet, paging, rowCap, ct).ConfigureAwait(false);
            if (response.IsFailure)
            {
                await SendFailure(response).ConfigureAwait(false);
                return;
            }

            await Send.OkAsync(response.Value!, ct).ConfigureAwait(false);
        }

        EndpointLogger.EndpointExecuted(Logger, GetType().Name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    /// <summary>Starts the streamed response of a caller no page limit applies to.</summary>
    /// <param name="req">The request.</param>
    /// <param name="stream">The opened stream, its first batch already read.</param>
    /// <returns>A response the stream's rows are written into, ending in <c>rowCount</c> and <c>complete</c>.</returns>
    protected virtual DataQueryStreamResponse NewStreamResponse(TRequest req, DataQueryStream stream)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(stream);

        return new DataQueryStreamResponse(stream, req.Skip);
    }

    private async Task SendFailure(IGenericResult failure)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(failure, HttpContext);
        await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
    }

    /// <summary>Resolves the data set the request's name reaches, or refuses.</summary>
    /// <param name="req">The request.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The data set, or the refusal the caller is answered with.</returns>
    protected abstract Task<IGenericResult<IDataSetImplementationConfiguration>> ResolveDataSet(TRequest req, CancellationToken ct);

    /// <summary>
    /// The filter the route puts on every query of the data set it resolved, added to the caller's own with AND, or null
    /// for none (FDW-1032). A route over a view returns the view's filter and its parents', so the view is the rows its
    /// filters keep. It is checked against the data set's fields with the caller's filter, so a stored filter on a field
    /// the data set lacks is refused by name.
    /// </summary>
    /// <param name="req">The request.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The filter, or the refusal the caller is answered with.</returns>
    protected virtual Task<IGenericResult<QueryFilterNode?>> ScopeFilter(TRequest req, CancellationToken ct)
        => Task.FromResult(GenericResult<QueryFilterNode?>.Success(null));

    /// <summary>Starts the response with whatever the route adds to a page.</summary>
    /// <param name="req">The request.</param>
    /// <returns>A response the page is written into.</returns>
    protected virtual TResponse NewResponse(TRequest req) => new();

    /// <summary>Resolves what every answer needs before any row is read: the data set, how the caller is paged and the calculated row cap.</summary>
    private async Task<IGenericResult<(IDataSetImplementationConfiguration DataSet, PaginatedListRequest Paging, int RowCap)>> Prepare(
        TRequest req, CancellationToken ct)
    {
        var dataSet = await ResolveDataSet(req, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<(IDataSetImplementationConfiguration, PaginatedListRequest, int)>();

        var paging = PaginationQuery.Resolve(HttpContext, Logger, req.Skip, req.Take);
        if (paging.IsFailure) return paging.ToNewResult<(IDataSetImplementationConfiguration, PaginatedListRequest, int)>();

        var rowCap = PaginationQuery.ResolveCalculatedRowCap(HttpContext, Logger);
        if (rowCap.IsFailure) return rowCap.ToNewResult<(IDataSetImplementationConfiguration, PaginatedListRequest, int)>();

        return GenericResult<(IDataSetImplementationConfiguration, PaginatedListRequest, int)>.Success(
            (dataSet.Value!, paging.Value!, rowCap.Value));
    }

    private async Task<IGenericResult<TResponse>> Query(
        TRequest req, IDataSetImplementationConfiguration dataSet, PaginatedListRequest paging, int rowCap, CancellationToken ct)
    {
        var page = await _query.Run(dataSet, req, paging.Take, rowCap, token => ScopeFilter(req, token), ct).ConfigureAwait(false);
        if (page.IsFailure) return page.ToNewResult<TResponse>();

        var read = page.Value!;
        var response = NewResponse(req);
        response.DataSetName = read.DataSetName;
        response.Columns = read.Columns;
        response.Rows = read.Rows;
        response.Skip = req.Skip;

        // Why the count and not the take: a grouped query no limit applies to asked for no page size, and the take it was cut
        // with is the largest number there is, which says nothing about the rows that follow.
        response.Take = paging.IsUnlimited ? read.Rows.Count : read.Take;
        response.HasMoreRows = read.HasMoreRows;
        response.GroupCount = read.GroupCount;
        return GenericResult<TResponse>.Success(response);
    }

    /// <summary>Answers a caller no page limit applies to by writing every row as it is read.</summary>
    private async Task Stream(TRequest req, IDataSetImplementationConfiguration dataSet, int rowCap, CancellationToken ct)
    {
        var batchSize = PaginationQuery.ResolveBatchSize(HttpContext, Logger);
        if (batchSize.IsFailure)
        {
            await SendFailure(batchSize).ConfigureAwait(false);
            return;
        }

        // Opened, and its first batch read, before the response starts: a refusal still answers with a status.
        var opened = await DataQueryStream.Open(
            _query, dataSet, req, batchSize.Value, rowCap, token => ScopeFilter(req, token), Logger, ct).ConfigureAwait(false);
        if (opened.IsFailure)
        {
            await SendFailure(opened).ConfigureAwait(false);
            return;
        }

        try
        {
            // Declared as object so the body is written as the type the route built (a workspace's response carries its name).
            await Send.ResultAsync(TypedResults.Json<object>(NewStreamResponse(req, opened.Value!))).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The client going away is the client's decision, and there is nobody left to tell.
            HttpContext.Abort();
        }
        catch (Exception ex)
        {
            // Why aborted, and never answered with a failure status: the serializer has already written some of the body into
            // the response, whether or not Kestrel has flushed it, and a failure written after it would be a second document in
            // the same body. A cut connection is what tells the client the rows are incomplete, where a closed body would read
            // as a whole one with a 200 on it. The stream logged its own failure; anything else is logged here.
            if (DataQueryStream.CausedBy(ex) is null)
                EndpointLogger.EndpointError(Logger, ex, GetType().Name);

            HttpContext.Abort();
        }
    }
}
