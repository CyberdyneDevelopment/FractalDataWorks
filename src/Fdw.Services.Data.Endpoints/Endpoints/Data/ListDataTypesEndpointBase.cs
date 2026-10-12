using System;
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
/// Lists the portable data types -- the <c>DataTypes</c> options a container field's
/// <c>FrameworkDataType</c> names (FDW-1103).
/// </summary>
public abstract class ListDataTypesEndpointBase : CrudListEndpointBase<DataTypeResponse>
{
    /// <summary>Initializes a new instance of the <see cref="ListDataTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected ListDataTypesEndpointBase(ILogger<ListDataTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the resource name used for route generation.</summary>
    protected override string ResourceName => "data-types";

    /// <inheritdoc />
    /// <remarks>
    /// Any signed-in caller may list the types: they are the platform's vocabulary, the same for every caller,
    /// and anyone who declares a field has to be able to name one. The same reasoning as the datum types.
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List data types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns every portable data type: the names a container field's FrameworkDataType and a data set field's dataType take, each with the .NET type it stands for.";

    /// <summary>Loads the types from the DataTypes type collection.</summary>
    protected override Task<IGenericResult<List<DataTypeResponse>>> LoadItems(CancellationToken ct)
    {
        var items = DataTypes.All()
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .Select(type => new DataTypeResponse { Name = type.Name, FullName = type.FullName })
            .ToList();
        return Task.FromResult(GenericResult<List<DataTypeResponse>>.Success(items));
    }
}
