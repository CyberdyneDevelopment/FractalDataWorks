using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Tier 2 default endpoint to get detailed configuration type information.
/// </summary>
/// <remarks>
/// Why: Wave C4 replaces ConfigurationTypes.GetByServiceType() with
/// IConfigurationContainerLookup.Get(). Property metadata (via CLR reflection) is not yet
/// available — IDataContainer does not carry the CLR Type (pending Wave A6). Returns empty
/// properties list until typed-body metadata is promoted to IDataContainer.
/// </remarks>
public abstract class GetConfigurationTypeDetailEndpointBase : CrudGetEndpointBase<GetTypeDetailRequest, ConfigurationTypeDetailDto>
{
    private readonly IConfigurationContainerLookup _containerLookup;

    /// <summary>Initializes a new instance of the endpoint.</summary>
    protected GetConfigurationTypeDetailEndpointBase(IConfigurationContainerLookup containerLookup)
    {
        _containerLookup = containerLookup;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "configuration-types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "configurations:read";

    /// <summary>Gets the route template for this endpoint.</summary>
    protected override string Route => "/configuration/types/detail";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "Get configuration type detail with properties";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns detailed property information for a specific configuration type. " +
        "Property metadata is extracted from the CLR type via reflection.";

    /// <summary>Returns the category and type as a combined resource identifier.</summary>
    protected override string GetResourceIdentifier(GetTypeDetailRequest request) => $"{request.Category}/{request.Type}";

    /// <summary>Finds and maps the configuration type detail for the requested category and type.</summary>
    protected override Task<IGenericResult<ConfigurationTypeDetailDto?>> FindByIdentifier(GetTypeDetailRequest request, CancellationToken ct)
    {
        var detail = FindConfigurationType(request.Category, request.Type);
        return Task.FromResult(GenericResult<ConfigurationTypeDetailDto?>.Success(detail));
    }

    /// <summary>Looks up and maps a container by service type name.</summary>
    protected virtual ConfigurationTypeDetailDto? FindConfigurationType(string category, string type)
    {
        var result = _containerLookup.Get(type);
        if (!result.IsSuccess)
            return null;

        return ConfigurationTypeMapper.ToDetail(result.Value!);
    }
}
