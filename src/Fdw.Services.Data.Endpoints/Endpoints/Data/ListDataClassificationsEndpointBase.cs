using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Lists the data classification levels, least sensitive first, from the <c>DataClassifications</c>
/// type collection (FDW-938).
/// </summary>
public abstract class ListDataClassificationsEndpointBase : CrudListEndpointBase<DataClassificationResponse>
{
    /// <summary>Initializes a new instance of the <see cref="ListDataClassificationsEndpointBase"/> class.</summary>
    protected ListDataClassificationsEndpointBase(ILogger<ListDataClassificationsEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the resource name used for route generation.</summary>
    protected override string ResourceName => "data-classifications";

    /// <inheritdoc />
    /// <remarks>
    /// Any signed-in caller may list the levels: they are the platform's vocabulary, the same for every
    /// caller, and anyone who classifies a data store, data set or workspace has to be able to name one.
    /// No <c>data-classifications</c> permission exists to gate them with.
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List data classification levels";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns every data classification level, least sensitive first, with the ClassificationId to send when classifying data.";

    /// <summary>Loads the levels from the DataClassifications type collection.</summary>
    protected override Task<IGenericResult<List<DataClassificationResponse>>> LoadItems(CancellationToken ct)
    {
        var items = DataClassifications.All()
            .OrderBy(level => level.Sensitivity)
            .Select(level => new DataClassificationResponse
            {
                ClassificationId = level.Id,
                Name = level.Name,
                Description = level.Description,
                Sensitivity = level.Sensitivity,
                RequiresInvitation = level.RequiresInvitation,
            })
            .ToList();

        return Task.FromResult(GenericResult<List<DataClassificationResponse>>.Success(items));
    }
}
