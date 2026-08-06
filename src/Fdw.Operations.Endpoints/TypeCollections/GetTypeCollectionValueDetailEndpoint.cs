using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Operations.Endpoints.ConfigurationMetadata;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Endpoint that returns full metadata for a specific TypeOption in a TypeCollection.
/// Route: GET /api/v1/type-collections/{collectionName}/values/{typeName}
/// </summary>
public abstract class GetTypeCollectionValueDetailEndpoint
    : CrudGetEndpoint<GetTypeCollectionValueRequest, TypeCollectionValueDetailDto>
{
    /// <inheritdoc/>
    protected override string ResourceName => "type-collections";

    /// <inheritdoc/>
    protected override string ReadPolicy => "configurations:read";

    /// <inheritdoc/>
    protected override string Route => "/type-collections/{collectionName}/values/{typeName}";

    /// <inheritdoc/>
    protected override string EndpointSummary => "Get TypeCollection value detail";

    /// <inheritdoc/>
    protected override string EndpointDescription =>
        "Returns full metadata for a specific TypeOption, including configuration property details.";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(GetTypeCollectionValueRequest request)
        => $"{request.CollectionName}/{request.TypeName}";

    /// <inheritdoc/>
    protected override Task<IGenericResult<TypeCollectionValueDetailDto?>> FindByIdentifier(
        GetTypeCollectionValueRequest request, CancellationToken ct)
    {
        var detail = FindTypeOptionDetail(request.CollectionName, request.TypeName);
        return Task.FromResult(GenericResult<TypeCollectionValueDetailDto?>.Success(detail));
    }

    private static TypeCollectionValueDetailDto? FindTypeOptionDetail(string collectionName, string typeName)
    {
        var collectionType = TypeCollectionResolver.FindCollectionType(collectionName);
        if (collectionType == null)
            return null;

        var typeOption = TypeCollectionResolver.GetValues(collectionType)
            .FirstOrDefault(v => string.Equals(v.Name, typeName, StringComparison.OrdinalIgnoreCase));

        if (typeOption == null)
            return null;

        var summary = TypeCollectionResolver.ToSummary(typeOption);
        var propertyMetadata = FindPropertyMetadata(collectionName, typeName);

        return new TypeCollectionValueDetailDto
        {
            Name = summary.Name,
            ExpectedProperties = summary.ExpectedProperties,
            RequiredProperties = summary.RequiredProperties,
            PropertyMetadata = propertyMetadata
        };
    }

    private static IReadOnlyList<ConfigurationPropertyInfoDto> FindPropertyMetadata(
        string collectionName, string typeName)
    {
        // Why: IConfigurationType.Type (CLR type for reflection) is removed in Wave C4.
        // IDataContainer does not yet carry the CLR Type — pending Wave A6 typed-body promotion.
        // Return empty list until Wave A6 exposes the CLR type via IDataContainer.
        return [];
    }
}
