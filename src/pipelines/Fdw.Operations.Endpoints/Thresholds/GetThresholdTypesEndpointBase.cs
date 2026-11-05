using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Operations.Abstractions.Thresholds;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>
/// Lists the kinds of threshold (FDW-3948): for each, what it judges, the kind of subject it is attached to and the values a rule of it may name. Read from the ThresholdTypes
/// collection, so a new kind appears here without a change to this route or to a screen.
/// </summary>
public abstract class GetThresholdTypesEndpointBase : EndpointWithoutRequest<ThresholdTypesResponse>
{
    /// <summary>Gets the policy a caller holds to read the types.</summary>
    protected virtual string ReadPolicy => "thresholds:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/thresholds/types");
        Policies(ReadPolicy);
        Description(x => x.WithTags("thresholds"));
        Summary(s =>
        {
            s.Summary = "List the threshold types";
            s.Description = "Each type names the kind of subject it is attached to and the fields a rule of it may name, each with the type of its value, its unit and what it says.";
        });
    }

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync(
            new ThresholdTypesResponse
            {
                Types = [.. ThresholdTypes.All()
                    .OrderBy(type => type.Name, System.StringComparer.Ordinal)
                    .Select(type => new ThresholdTypeResponse
                    {
                        Name = type.Name,
                        Description = type.Description,
                        SubjectKind = type.SubjectKind,
                        Parameters = [.. type.Parameters.Select(parameter => new ThresholdParameterResponse
                        {
                            Name = parameter.Name,
                            ValueType = parameter.ValueType.Name,
                            IsRequired = parameter.IsRequired,
                            IsList = parameter.IsList,
                            Description = parameter.Description,
                        })],
                        Fields = [.. type.Fields.Select(field => new ThresholdFieldResponse
                        {
                            Name = field.Name,
                            ValueType = field.ValueType.Name,
                            Unit = field.Unit,
                            Description = field.Description,
                            Operators = [.. field.Operators],
                        })],
                    })],
            },
            ct);
}
