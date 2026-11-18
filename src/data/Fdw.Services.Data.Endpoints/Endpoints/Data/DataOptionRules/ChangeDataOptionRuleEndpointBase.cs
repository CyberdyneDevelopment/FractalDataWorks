using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>PATCH /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}</c> (datastores:write), FDW-6339. Changes a rule; a field left out keeps the stored one. Only the custodian or a holder of the rule's permission may.
/// Built by FDW-6489: it answers through the rule service.
/// </summary>
public abstract class ChangeDataOptionRuleEndpointBase : DataOptionRulePayloadEndpointBase<ChangeDataOptionRuleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ChangeDataOptionRuleEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected ChangeDataOptionRuleEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger, rules, names)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "ChangeDataOptionRule";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRulePayload>> Run(ChangeDataOptionRuleRequest request, CancellationToken cancellationToken)
    {
        if (request.Change is not { } body)
            return Refused(DataOptionRulesEndpointLog.RuleBodyMissing(EndpointLogger, RouteName, "change"));

        return await Payload(await Rules.Change(DataOptionRuleMapping.RuleChange(request, body), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "Change a data option rule";
            s.Description = "Changes a rule; a field left out keeps the stored one. Only the custodian or a holder of the rule's permission may.";
        });
        ConfigureEndpoint();
    }
}
