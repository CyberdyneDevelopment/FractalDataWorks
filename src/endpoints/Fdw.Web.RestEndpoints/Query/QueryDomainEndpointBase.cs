using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.Http.Abstractions.EndPoints;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// Queries one domain's list: <c>POST /{resource}/query</c>. A filter tree, an ordering, a page and the
/// fields wanted travel in the body; the page is answered from the domain table alone.
/// </summary>
/// <typeparam name="TDomain">The domain's record: the four properties every domain has, plus the fields its list shows.</typeparam>
/// <remarks>
/// <para>
/// The fields a query may name, filter on, sort by or ask for are exactly the ones the resource declares in
/// <see cref="QueryableFields"/>, each with the column it reaches and the type its values have. Nothing is
/// derived from what the record happens to carry, so adding a property to the record exposes nothing, and a
/// tenancy, visibility or secret column is queryable only if someone writes it into the list. A declared
/// column the record does not have is refused at the first query, loudly, not skipped.
/// </para>
/// <para>
/// This is the answer to what a list <c>GET</c> cannot say: which members, in what order, only these
/// fields, and how many in all. It carries the same envelope as the list, so a client reads one shape:
/// <c>skip</c>, <c>take</c>, <c>hasMore</c>, <c>total</c> when asked, and the <c>items</c>, each keyed by
/// the field's camel-case name. The query runs under the caller's own session, so the store's row-level
/// security narrows the rows to the caller.
/// </para>
/// </remarks>
public abstract class QueryDomainEndpointBase<TDomain>
    : Endpoint<ResourceQueryRequest, StreamedPageResponse<IReadOnlyDictionary<string, object?>>>
    where TDomain : DomainConfiguration
{
    private readonly IDomainRecordProvider<TDomain> _records;

    /// <summary>Initializes a new instance of the <see cref="QueryDomainEndpointBase{TDomain}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="records">Reads the page from the domain table.</param>
    protected QueryDomainEndpointBase(ILogger logger, IDomainRecordProvider<TDomain> records)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _records = records ?? throw new ArgumentNullException(nameof(records));
    }

    /// <summary>Gets the plural resource name used for routing and policy generation (e.g., "users").</summary>
    protected abstract string ResourceName { get; }

    /// <summary>
    /// Gets the fields a query over this resource may filter on, sort by and ask for, each with the column it
    /// reaches and the type its values have.
    /// </summary>
    /// <remarks>
    /// Stated by the resource where it is registered, not derived: what an API exposes is a decision, and a
    /// column that must never be queried (tenancy, visibility, a secret) stays out by being left out.
    /// </remarks>
    protected abstract IReadOnlyList<QueryableField> QueryableFields { get; }

    /// <summary>
    /// Gets a predicate every query over this resource is held to, or null when it holds none.
    /// </summary>
    /// <remarks>
    /// For a resource that shares its domain table with another (data views and scoped variations are both
    /// <c>DataSetVariation</c> rows): the caller's own filter is AND-ed with this one, so a caller can narrow the
    /// resource and never widen it. It is not a caller-visible field, so it cannot be queried, only obeyed.
    /// </remarks>
    protected virtual IFilterExpression? ScopeFilter => null;

    /// <summary>Gets the read policy for this endpoint. Defaults to "{ResourceName}:read", the list's own.</summary>
    protected virtual string ReadPolicy => $"{ResourceName}:read";

    /// <summary>Gets the route for this endpoint. Defaults to "/{ResourceName}/query".</summary>
    protected virtual string Route => $"/{ResourceName}/query";

    /// <summary>Gets the logger instance.</summary>
    protected new ILogger Logger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Options(builder => builder.WithMetadata(StreamingResponseMetadata.Instance));

        Summary(s =>
        {
            s.Summary = $"Query {ResourceName}";
            s.Description =
                $"Queries the {ResourceName} list: a filter tree, an ordering, a page and the fields wanted travel in the body. " +
                "take defaults to and is capped by the pagination settings; total is returned only when includeTotal is true. " +
                "Answered from the domain table alone, under the caller's own session.";
        });

        Description(x => x.WithTags(ResourceName));
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ResourceQueryRequest req, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        EndpointLogger.ExecutingEndpoint(Logger, GetType().Name, nameof(ResourceQueryRequest));

        try
        {
            var answer = await Query(req, ct).ConfigureAwait(false);
            if (answer.IsFailure)
            {
                await SendErrorResponse(answer, ct).ConfigureAwait(false);
                return;
            }

            var (page, mapper, selected) = answer.Value;
            var response = new StreamedPageResponse<IReadOnlyDictionary<string, object?>>(
                page.Skip, page.Take, page.HasMore, Rows(page, mapper, selected, ct), () => Error(page.Outcome), page.Total);

            await Send.ResultAsync(TypedResults.Json(response)).ConfigureAwait(false);
            EndpointLogger.EndpointExecuted(Logger, GetType().Name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var msg = EndpointLogger.EndpointError(Logger, ex, GetType().Name);

            // Once the first byte is out the status code is fixed; a cut connection is what tells the
            // client the page is incomplete, where a closed body would read as a whole one.
            if (HttpContext.Response.HasStarted)
            {
                HttpContext.Abort();
                return;
            }

            await SendErrorResponse(GenericResult.Failure(msg), ct).ConfigureAwait(false);
        }
    }

    private async Task<IGenericResult<(ConfigurationPage<TDomain> Page, IPocoMapper Mapper, IReadOnlyList<QueryableField> Selected)>> Query(
        ResourceQueryRequest req, CancellationToken ct)
    {
        var mapper = PocoMapperCollection.ByName(typeof(TDomain).Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<(ConfigurationPage<TDomain>, IPocoMapper, IReadOnlyList<QueryableField>)>.Failure(
                EndpointLogger.QueryFieldsUnavailable(Logger, ResourceName, typeof(TDomain).Name));

        var declared = QueryableFields;
        var valid = Validate(declared, mapper);
        if (valid.IsFailure) return valid.ToNewResult<(ConfigurationPage<TDomain>, IPocoMapper, IReadOnlyList<QueryableField>)>();

        var refusals = new ResourceQueryRefusals(ResourceName, Logger);
        var translator = QueryTranslator.Create(declared, ResourceName, refusals);
        if (translator.IsFailure) return translator.ToNewResult<(ConfigurationPage<TDomain>, IPocoMapper, IReadOnlyList<QueryableField>)>();

        var selected = Selected(req.Fields, declared, refusals);
        if (selected.IsFailure) return selected.ToNewResult<(ConfigurationPage<TDomain>, IPocoMapper, IReadOnlyList<QueryableField>)>();

        var paging = PaginationQuery.Resolve(HttpContext, Logger, req.Skip, req.Take);
        if (paging.IsFailure) return paging.ToNewResult<(ConfigurationPage<TDomain>, IPocoMapper, IReadOnlyList<QueryableField>)>();

        var query = translator.Value!.ToDomainQuery(req, paging.Value!.Take);
        if (query.IsFailure) return query.ToNewResult<(ConfigurationPage<TDomain>, IPocoMapper, IReadOnlyList<QueryableField>)>();

        var page = await _records.Get(Scoped(query.Value!), ct).ConfigureAwait(false);
        return page.IsFailure
            ? page.ToNewResult<(ConfigurationPage<TDomain>, IPocoMapper, IReadOnlyList<QueryableField>)>()
            : GenericResult<(ConfigurationPage<TDomain>, IPocoMapper, IReadOnlyList<QueryableField>)>.Success(
                (page.Value!, mapper, selected.Value!));
    }

    // The caller's query held to the resource's own predicate: AND-ed, so the scope can only narrow.
    private DomainQuery Scoped(DomainQuery query)
    {
        if (ScopeFilter?.Root is not { } scope) return query;

        IFilterNode root = query.Filter?.Root is { } asked
            ? new FilterGroup { Operator = LogicalOperator.And, Nodes = [asked, scope] }
            : scope;
        return new DomainQuery(new FilterExpression { Root = root }, query.Ordering, query.Skip, query.Take, query.IncludeTotal);
    }

    // A declaration that does not fit the record is the resource's defect, not the caller's: it is refused
    // as a failure of the resource, never narrowed to the fields that do fit.
    private IGenericResult Validate(IReadOnlyList<QueryableField> declared, IPocoMapper mapper)
    {
        if (declared.Count == 0)
            return GenericResult.Failure(EndpointLogger.QueryableFieldsInvalid(Logger, ResourceName, "it declares none"));

        var columns = mapper.GetPropertyNames();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in declared)
        {
            if (!columns.Contains(field.Column, StringComparer.Ordinal))
                return GenericResult.Failure(EndpointLogger.QueryableFieldsInvalid(
                    Logger, ResourceName, $"'{field.Name}' names column '{field.Column}', which {typeof(TDomain).Name} does not have"));

            if (!seen.Add(field.Name))
                return GenericResult.Failure(EndpointLogger.QueryableFieldsInvalid(
                    Logger, ResourceName, $"'{field.Name}' is declared more than once"));
        }

        return GenericResult.Success();
    }

    // The fields the caller asked for, or every declared field when it asked for none.
    private IGenericResult<IReadOnlyList<QueryableField>> Selected(
        IReadOnlyList<string?>? asked, IReadOnlyList<QueryableField> declared, ResourceQueryRefusals refusals)
    {
        if (asked is not { Count: > 0 })
            return GenericResult<IReadOnlyList<QueryableField>>.Success(declared);

        var picked = new List<QueryableField>();
        foreach (var name in asked)
        {
            var field = declared.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            if (field is null)
                return refusals.Invalid<IReadOnlyList<QueryableField>>($"field '{name}' is not a field of {ResourceName}");
            if (!picked.Contains(field))
                picked.Add(field);
        }

        return GenericResult<IReadOnlyList<QueryableField>>.Success(picked);
    }

    private static async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> Rows(
        ConfigurationPage<TDomain> page,
        IPocoMapper mapper,
        IReadOnlyList<QueryableField> selected,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var record in page.Items.WithCancellation(ct).ConfigureAwait(false))
        {
            var values = mapper.MapToParameters(record);
            var row = new Dictionary<string, object?>(selected.Count, StringComparer.Ordinal);
            foreach (var field in selected)
                row[field.Name] = values[field.Column];

            yield return row;
        }
    }

    private Microsoft.AspNetCore.Mvc.ProblemDetails? Error(IGenericResult outcome)
        => outcome.IsFailure ? ResultHttpStatusMapper.Map(outcome, HttpContext).Response : null;

    private async Task SendErrorResponse(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
    }
}
