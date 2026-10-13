using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Lists the formats a container can state (<c>GET /format-types</c>): each with the family that reads and writes it, its file
/// extension and the separator it defines, so a form shows the right options without a list of its own (FDW-1315).
/// </summary>
public abstract class ListFormatTypesEndpointBase : CrudListEndpointBase<FormatTypePayload>
{
    /// <summary>Initializes a new instance of the <see cref="ListFormatTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected ListFormatTypesEndpointBase(ILogger<ListFormatTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "format-types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "datastores:read";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List the container formats";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns each format a container can state, with its family, file extension and the separator it defines.";

    /// <summary>Loads every registered format as a payload.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The formats, without the sentinel for a name that did not resolve.</returns>
    protected override Task<IGenericResult<List<FormatTypePayload>>> LoadItems(CancellationToken ct)
    {
        FormatTypeEndpointLog.ListingFormatTypes(Logger);

        var items = FormatTypes.All()
            .Where(f => !ReferenceEquals(f, FormatTypes.NotFound))
            .OrderBy(f => f.Name, System.StringComparer.Ordinal)
            .Select(f => new FormatTypePayload
            {
                Name = f.Name,
                DisplayName = f.DisplayName,
                Description = f.Description,
                Family = f.Family,
                MediaType = f.MimeType,
                IsBinary = f.IsBinary,
                SupportsStreaming = f.SupportsStreaming,
                FileExtension = f.CanonicalFileExtension,
                Separator = f.Separator
            })
            .ToList();

        FormatTypeEndpointLog.ListedFormatTypes(Logger, items.Count);
        return Task.FromResult(GenericResult<List<FormatTypePayload>>.Success(items));
    }
}
