using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The Field kind: one operation applied to one field's value, with the parameter values that operation declares. It takes
/// <c>transformationType</c> and <c>parameters</c>, and refuses the members of the Built kind.
/// </summary>
[TypeOption(typeof(TransformationKinds), "Field", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldTransformationKind : TransformationKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FieldTransformationKind"/> class.</summary>
    public FieldTransformationKind() : base("Field")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<ITransformationImplementationConfiguration> Prepare(
        CreateTransformationRequest request, TransformationKindContext context)
    {
        if (request.Inputs is not null)
        {
            return Refused(TransformationKindLog.MemberNotForKind(context.Logger, Name, "inputs"));
        }

        if (request.Results is not null)
        {
            return Refused(TransformationKindLog.MemberNotForKind(context.Logger, Name, "results"));
        }

        if (string.IsNullOrWhiteSpace(request.TransformationType))
        {
            return Refused(TransformationKindLog.MemberRequired(context.Logger, Name, "transformationType"));
        }

        var valid = TransformationEndpointSupport.Validate(request.TransformationType, request.Parameters, context.Logger);
        return valid.IsFailure
            ? valid.ToNewResult<ITransformationImplementationConfiguration>()
            : GenericResult<ITransformationImplementationConfiguration>.Success(new FieldTransformationConfiguration
            {
                Description = request.Description,
                TransformationType = request.TransformationType,
                IsSystemProvided = false,
                Parameters = [.. request.Parameters.Select(p => new FieldTransformationParameterConfiguration { Name = p.Name, Value = p.Value })],
            });
    }

    /// <inheritdoc />
    public override IGenericResult<ITransformationImplementationConfiguration> PrepareUpdate(
        UpdateTransformationRequest request, ITransformationImplementationConfiguration existing, TransformationKindContext context)
    {
        if (request.Inputs is not null || request.Results is not null)
        {
            return Refused(TransformationKindLog.MemberNotForKind(context.Logger, Name, request.Inputs is not null ? "inputs" : "results"));
        }

        if (existing is not IFieldTransformationConfiguration field)
        {
            return Refused(FieldMappingTransformEndpointLog.TransformationNotFieldLevel(context.Logger, existing.Name, existing.Implementation));
        }

        var type = request.TransformationType ?? field.TransformationType;
        var parameters = request.Parameters
            ?? [.. field.Parameters.Select(p => new TransformationParameterPayload { Name = p.Name, Value = p.Value })];
        var valid = TransformationEndpointSupport.Validate(type, parameters, context.Logger);
        return valid.IsFailure
            ? valid.ToNewResult<ITransformationImplementationConfiguration>()
            : GenericResult<ITransformationImplementationConfiguration>.Success(new FieldTransformationConfiguration
            {
                // A new instance rather than the stored one: that one may be the provider's cached copy.
                Id = field.Id,
                Description = request.Description ?? field.Description,
                TransformationType = type,
                IsSystemProvided = false,
                Parameters = [.. parameters.Select(p => new FieldTransformationParameterConfiguration { Name = p.Name, Value = p.Value })],
            });
    }

    /// <inheritdoc />
    public override void Describe(ITransformationImplementationConfiguration transformation, TransformationPayload payload)
    {
        if (transformation is not IFieldTransformationConfiguration field)
        {
            return;
        }

        payload.TransformationType = field.TransformationType;
        payload.Parameters = TransformationEndpointSupport.ToParameterPayloads(field.Parameters);
    }

    private static IGenericResult<ITransformationImplementationConfiguration> Refused(IGenericMessage message) =>
        GenericResult<ITransformationImplementationConfiguration>.Failure(message);
}
