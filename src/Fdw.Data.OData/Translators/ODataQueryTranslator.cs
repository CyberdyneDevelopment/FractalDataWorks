using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.OData.Results;
using Fdw.Results;

namespace Fdw.Data.OData;

/// <summary>
/// Translates QueryCommand to REST GET request with OData query parameters.
/// </summary>
/// <remarks>
/// <para>
/// Builds HTTP GET requests with OData query conventions:
/// <list type="bullet">
/// <item>$filter - WHERE clause equivalent (e.g., "Name eq 'Acme' and IsActive eq true")</item>
/// <item>$select - SELECT clause equivalent (e.g., "Name,IsActive,CreatedDate")</item>
/// <item>$orderby - ORDER BY equivalent (e.g., "Name asc, CreatedDate desc")</item>
/// <item>$top - TAKE/LIMIT equivalent (e.g., "50")</item>
/// <item>$skip - SKIP/OFFSET equivalent (e.g., "100")</item>
/// </list>
/// </para>
/// <para>
/// Uses FilterOperator.ODataOperator properties to avoid switch statements - each operator
/// knows its OData representation (eq, ne, gt, lt, contains, etc.).
/// </para>
/// </remarks>
[TypeOption(typeof(ODataCommandTranslators), "ODataQuery", RestrictToCurrentCompilation = true)]
public sealed class ODataQueryTranslator : ODataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataQueryTranslator"/> class.
    /// </summary>
    public ODataQueryTranslator()
        : base("ODataQuery")
    {
    }

    /// <summary>
    /// Translates a QueryCommand to a REST GET request with OData parameters.
    /// </summary>
    /// <param name="command">The data command to translate.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the HttpRequestMessage.</returns>
    // MA0051: Method length acceptable - sequential translation algorithm (extract metadata, build query params, assemble HTTP request)
#pragma warning disable MA0051 // Method is too long
    [ConventionOverride(MaxCyclomaticComplexity = 25)]  // OData query translation with multiple expression types (filter, projection, ordering, paging) and conditional parameter building
    public override Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
#pragma warning restore MA0051
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                    GenericResult<HttpRequestMessage>.Failure(
                        ODataResultCodes.ByName("ContainerNull")));
            }

            // Get QueryCommand-specific properties via metadata
            var filter = command.Metadata?.TryGetValue("Filter", out var filterObj) == true
                ? filterObj as IFilterExpression
                : null;

            var projection = command.Metadata?.TryGetValue("Projection", out var projectionObj) == true
                ? projectionObj as IProjectionExpression
                : null;

            var ordering = command.Metadata?.TryGetValue("Ordering", out var orderingObj) == true
                ? orderingObj as IOrderingExpression
                : null;

            var paging = command.Metadata?.TryGetValue("Paging", out var pagingObj) == true
                ? pagingObj as IPagingExpression
                : null;

            // Build query parameters dictionary
            var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Add $filter parameter
            if (filter?.Root != null)
            {
                queryParams["$filter"] = BuildODataFilter(filter);
            }

            // Add $select parameter
            if (projection?.PropertyNames?.Any() == true)
            {
                queryParams["$select"] = BuildODataSelect(projection);
            }

            // Add $orderby parameter
            if (ordering?.OrderedFields?.Any() == true)
            {
                queryParams["$orderby"] = BuildODataOrderBy(ordering);
            }

            // Add $top and $skip parameters
            if (paging != null)
            {
                AddODataPaging(paging, queryParams);
            }

            // Build relative path from container name (e.g., "/api/Customers")
            var relativePath = container.Name.StartsWith('/')
                ? container.Name
                : $"/{container.Name}";

            // Build query string
            var queryString = string.Empty;
            if (queryParams.Count > 0)
            {
                queryString = "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            }

            // Get HTTP GET request
            var request = new HttpRequestMessage(HttpMethod.Get, relativePath + queryString);

            return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                GenericResult<HttpRequestMessage>.Success(request));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                GenericResult<HttpRequestMessage>.Failure(
                    ODataResultCodes.ByName("QueryTranslationFailed"),
                    ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }
}
