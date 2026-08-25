using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.Transformations;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for listing available field transform types from the TransformationTypes TypeCollection.
/// Enumerates all registered TransformationTypes, filters to FieldTransformationBase instances,
/// and maps each to a TransformTypePayload including parameter definitions.
/// </summary>
public abstract class ListTransformTypesEndpointBase : CrudListEndpointBase<TransformTypePayload>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "transform-types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "datasets:read";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List available field transform types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns a list of all available field transform types registered via the TransformationTypes TypeCollection.";

    /// <summary>Loads all registered field transform types as DTOs.</summary>
    protected override Task<IGenericResult<List<TransformTypePayload>>> LoadItems(CancellationToken ct)
    {
        FieldMappingTransformEndpointLog.ListingTransformTypes(Logger);

        var items = MapTransformTypes();

        FieldMappingTransformEndpointLog.ListedTransformTypes(Logger, items.Count);
        return Task.FromResult(GenericResult<List<TransformTypePayload>>.Success(items.ToList()));
    }

    /// <summary>
    /// Enumerates all TransformationTypes, filters to FieldTransformationBase instances,
    /// and maps each to a TransformTypePayload.
    /// </summary>
    protected virtual IReadOnlyList<TransformTypePayload> MapTransformTypes()
    {
        return TransformationTypes.All()
            .OfType<FieldTransformationBase>()
            .Select(MapToDto)
            .ToList();
    }

    /// <summary>Maps a single FieldTransformationBase to a TransformTypePayload.</summary>
    protected virtual TransformTypePayload MapToDto(FieldTransformationBase transformer)
    {
        return new TransformTypePayload
        {
            Name = transformer.Name,
            DisplayName = transformer.DisplayName,
            Description = transformer.Description,
            Category = transformer.Category,
            SupportsBatching = transformer.SupportsBatching,
            Parameters = transformer.ExpectedParameters
                .Select(p => new TransformParameterDefinitionPayload
                {
                    Name = p.Name,
                    Kind = p.Kind,
                    IsRequired = p.IsRequired,
                    DisplayName = p.DisplayName,
                    HelpText = p.HelpText
                })
                .ToList()
        };
    }
}
