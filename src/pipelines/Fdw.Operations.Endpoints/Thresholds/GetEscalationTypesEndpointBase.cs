using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Operations.Abstractions.Thresholds;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>
/// Lists the consequences a rule can fire (FDW-3948), each with what a rule of it must state: an escalation policy, notification rules, or neither. Read from the
/// EscalationTypes collection, so the picker asks only for what the chosen consequence needs.
/// </summary>
public abstract class GetEscalationTypesEndpointBase : EndpointWithoutRequest<EscalationTypesResponse>
{
    /// <summary>Gets the policy a caller holds to read the types.</summary>
    protected virtual string ReadPolicy => "thresholds:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/thresholds/escalation-types");
        Policies(ReadPolicy);
        Description(x => x.WithTags("thresholds"));
        Summary(s =>
        {
            s.Summary = "List the escalation types";
            s.Description = "Each type says what it does and whether a rule of it must name an escalation policy, notification rules, and whether it acts on the run.";
        });
    }

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync(
            new EscalationTypesResponse
            {
                Types = [.. EscalationTypes.All()
                    .OrderBy(type => type.Name, System.StringComparer.Ordinal)
                    .Select(type => new EscalationTypeResponse
                    {
                        Name = type.Name,
                        Description = type.Description,
                        NeedsPolicy = type.NeedsPolicy,
                        NeedsNotificationRules = type.NeedsNotificationRules,
                        ActsOnRun = type.ActsOnRun,
                    })],
            },
            ct);
}
