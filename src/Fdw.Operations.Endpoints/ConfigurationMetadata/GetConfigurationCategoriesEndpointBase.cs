using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Tier 2 default endpoint to list all available configuration categories.
/// </summary>
/// <remarks>
/// Why: Wave C4 replaces ConfigurationTypes.All() with IConfigurationContainerLookup.All().
/// Categories are derived from container path names (schemas) until Wave A6 adds
/// SectionPath/ServiceCategory metadata to IDataContainer.
/// </remarks>
public abstract class GetConfigurationCategoriesEndpointBase : CrudListEndpointBase<string>
{
    private readonly IConfigurationContainerLookup _containerLookup;

    /// <summary>Initializes a new instance of the endpoint.</summary>
    protected GetConfigurationCategoriesEndpointBase(IConfigurationContainerLookup containerLookup)
    {
        _containerLookup = containerLookup;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "configuration-categories";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "configurations:read";

    /// <summary>Gets the route template for this endpoint.</summary>
    protected override string Route => "/configuration/categories";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List configuration categories";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns all available configuration categories discovered from registered types.";

    /// <summary>Loads all distinct configuration categories.</summary>
    protected override Task<IGenericResult<List<string>>> LoadItems(CancellationToken ct)
    {
        var items = MapCategories();
        return Task.FromResult(GenericResult<List<string>>.Success(items.ToList()));
    }

    /// <summary>
    /// Extracts distinct, sorted category names from all containers in the ctrl tree.
    /// </summary>
    /// <remarks>
    /// Why: IDataContainer does not yet surface ServiceCategory (pending Wave A6 typed-body
    /// promotion). Uses Path.Name (schema) as a proxy for category until Wave A6 ships.
    /// </remarks>
    protected virtual IReadOnlyList<string> MapCategories()
    {
        return _containerLookup.All()
            .Select(c => c.Parent.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();
    }
}
