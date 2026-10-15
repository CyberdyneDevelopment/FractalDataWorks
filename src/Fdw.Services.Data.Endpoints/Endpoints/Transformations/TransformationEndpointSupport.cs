using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Mapping and validation shared by the transformation endpoints.</summary>
internal static class TransformationEndpointSupport
{
    /// <summary>The transformation as the API shows it.</summary>
    /// <remarks>
    /// The operation and its parameters are a Field transformation's own; a set kind has neither, and
    /// its payload says which kind it is through <see cref="TransformationPayload.Implementation"/>.
    /// </remarks>
    /// <summary>Describes a stored transformation as the summary a list shows: what every kind has, then what its own kind adds.</summary>
    internal static TransformationPayload ToPayload(ITransformationImplementationConfiguration transformation)
    {
        var payload = new TransformationPayload
        {
            Name = transformation.Name,
            Description = transformation.Description,
            Implementation = transformation.Implementation,
            IsSystemProvided = transformation.IsSystemProvided,
        };
        if (KindOf(transformation.Implementation) is { } kind)
        {
            kind.Describe(transformation, payload);
        }

        return payload;
    }

    /// <summary>Describes a stored transformation as the read of one shows it, formulas included.</summary>
    internal static IGenericResult<TransformationPayload> ToDetail(
        ITransformationImplementationConfiguration transformation, TransformationKindContext context)
    {
        var payload = ToPayload(transformation);
        if (KindOf(transformation.Implementation) is not { } kind)
        {
            return GenericResult<TransformationPayload>.Success(payload);
        }

        var detailed = kind.Detail(transformation, payload, context);
        return detailed.IsFailure
            ? detailed.ToNewResult<TransformationPayload>()
            : GenericResult<TransformationPayload>.Success(payload);
    }

    /// <summary>Finds the kind a request or a stored transformation names; a kind nothing authors here is refused.</summary>
    internal static IGenericResult<TransformationKindBase> FindKind(string? implementation, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(implementation))
        {
            return GenericResult<TransformationKindBase>.Failure(TransformationKindLog.ImplementationMissing(logger));
        }

        return KindOf(implementation) is { } kind
            ? GenericResult<TransformationKindBase>.Success(kind)
            : GenericResult<TransformationKindBase>.Failure(TransformationKindLog.ImplementationUnknown(logger, implementation));
    }

    private static TransformationKindBase? KindOf(string implementation)
    {
        // ByName answers the collection's interface; the kinds all derive from the base, and a name nothing is registered for
        // answers the collection's NotFound option.
        return TransformationKinds.ByName(implementation) is TransformationKindBase option
            && !ReferenceEquals(option, TransformationKinds.NotFound)
            ? option
            : null;
    }

    internal static IReadOnlyList<TransformationParameterPayload> ToParameterPayloads(
        IEnumerable<FieldTransformationParameterConfiguration> parameters)
        => [.. parameters.Select(p => new TransformationParameterPayload { Name = p.Name, Value = p.Value })];

    /// <summary>
    /// Checks that <paramref name="transformationType"/> is a registered per-field operation, that every
    /// parameter it requires is given, and that nothing it does not declare is.
    /// </summary>
    internal static IGenericResult Validate(
        string transformationType, IEnumerable<TransformationParameterPayload> parameters, ILogger logger)
    {
        if (TransformationTypes.ByName(transformationType) is not FieldTransformationBase operation)
            return GenericResult.Failure(
                FieldMappingTransformationEndpointLog.TransformationTypeUnknown(logger, transformationType));

        var given = parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var declared = operation.ExpectedParameters.Select(d => d.Name).ToHashSet(StringComparer.Ordinal);

        var missing = operation.ExpectedParameters.FirstOrDefault(d => d.IsRequired && !given.Contains(d.Name));
        if (missing is not null)
            return GenericResult.Failure(
                FieldMappingTransformationEndpointLog.TransformationParameterMissing(logger, transformationType, missing.Name));

        var unknown = given.FirstOrDefault(name => !declared.Contains(name));
        if (unknown is not null)
            return GenericResult.Failure(
                FieldMappingTransformationEndpointLog.TransformationParameterUnknown(logger, transformationType, unknown));

        return GenericResult.Success();
    }
}
