using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// A data option rule route that answers through the rule service (FDW-6489): it reads the request, runs one service call, and sends the answer; a failure is answered by the
/// shared result-to-HTTP mapper with the failure's own code and category. The routes that are not built yet keep <see cref="DataOptionRuleContractEndpointBase{TRequest,TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">The request.</typeparam>
/// <typeparam name="TResponse">The answer.</typeparam>
public abstract class DataOptionRuleServiceEndpointBase<TRequest, TResponse> : DataOptionRuleContractEndpointBase<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionRuleServiceEndpointBase{TRequest,TResponse}"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected DataOptionRuleServiceEndpointBase(ILogger? logger)
        : base(logger)
    {
    }

    private const int FirstPage = 1;

    /// <summary>Gets the status a success is sent with.</summary>
    protected virtual int SuccessStatus => 200;

    /// <summary>Runs the route: the service call and the mapping of its answer.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The answer; or why there is none.</returns>
    protected abstract Task<IGenericResult<TResponse>> Run(TRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Works out the window of a list from the request's page and page size with the shared page resolver, which sizes it from the pagination settings and refuses a page or size
    /// below one with its own code. A page left out is the first page, as it is on every other list that pages by number.
    /// </summary>
    /// <param name="page">The page the request asks for, counted from one.</param>
    /// <param name="pageSize">The page size the request asks for.</param>
    /// <returns>The window; or why the request cannot have one.</returns>
    protected IGenericResult<DataOptionRulePageWindow> Window(int? page, int? pageSize)
    {
        var asked = page is { } number ? number : FirstPage;
        var paging = PaginationQuery.ResolvePage(HttpContext, EndpointLogger, asked, pageSize);
        return paging.IsSuccess && paging.Value is { } resolved
            ? GenericResult<DataOptionRulePageWindow>.Success(new DataOptionRulePageWindow(resolved.Skip, resolved.Take, asked))
            : paging.ToNewResult<DataOptionRulePageWindow>();
    }

    /// <inheritdoc />
    public sealed override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var result = await Run(req, ct).ConfigureAwait(false);
        if (result.IsFailure || result.Value is not { } answer)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(answer, SuccessStatus, ct).ConfigureAwait(false);
    }
}
