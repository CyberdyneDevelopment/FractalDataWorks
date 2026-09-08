using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions;
using Fdw.Services.Quality.Abstractions.TypeCollections.QualitySeverityTypeOptions;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Lists the rule types and severities this host has registered.</summary>
/// <remarks>
/// Answers from the TypeCollections rather than a literal list, so it reports what the host can
/// actually accept. A client that hardcodes the set goes stale the moment a type is added, and a
/// rule stored with an unrecognised type sits in the list looking correct and never runs.
/// </remarks>
public abstract class GetQualityRuleTypesEndpointBase : EndpointWithoutRequest<QualityRuleTypesResponse>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected virtual string ResourceName => "quality/rule-types";

    /// <summary>Gets the read policy for this endpoint.</summary>
    protected virtual string ReadPolicy => "quality/rules:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get($"/{ResourceName}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "List quality rule types and severities";
            s.Description = "Returns the rule types and severities registered in this host, so a "
                          + "client can offer exactly what the server will accept.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(new QualityRuleTypesResponse
        {
            // Ordered by name rather than by id: the ids are registration order and carry no
            // meaning for a reader, and a stable alphabetical list keeps a picker from reshuffling
            // when a type is added.
            RuleTypes = QualityRuleTypes.All()
                .Select(t => new QualityRuleTypeDto
                {
                    Name = t.Name,
                    Description = t.Description,
                    RequiresField = t.RequiresField,
                    SupportsMultipleFields = t.SupportsMultipleFields,
                    RequiresParameters = t.RequiresParameters,
                })
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList(),

            Severities = QualitySeverityTypes.All()
                .Select(s => new QualitySeverityDto
                {
                    Name = s.Name,
                    BlocksProcessing = s.BlocksProcessing,
                })
                .OrderBy(s => s.Name, StringComparer.Ordinal)
                .ToList(),
        }, ct).ConfigureAwait(false);
    }
}
