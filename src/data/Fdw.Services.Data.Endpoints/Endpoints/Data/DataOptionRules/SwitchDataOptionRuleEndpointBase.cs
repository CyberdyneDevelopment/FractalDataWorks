using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/active</c> (datastores:write), FDW-6339. Switches a rule on or off; a rule that is off is kept and does nothing.
/// Built by FDW-6489: it answers through the rule service.
/// </summary>
public abstract class SwitchDataOptionRuleEndpointBase : DataOptionRulePayloadEndpointBase<SwitchDataOptionRuleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SwitchDataOptionRuleEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected SwitchDataOptionRuleEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger, rules, names)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "SwitchDataOptionRule";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRulePayload>> Run(SwitchDataOptionRuleRequest request, CancellationToken cancellationToken)
    {
        if (request.Switch is not { Active: { } active })
            return Refused(DataOptionRulesEndpointLog.RuleFieldMissing(EndpointLogger, RouteName, "active"));

        return await Payload(await Rules.Switch(DataOptionRuleMapping.Key(request), active, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/active");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "Switch a data option rule on or off";
            s.Description = "Switches a rule on or off; a rule that is off is kept and does nothing.";
        });
        ConfigureEndpoint();
    }
}
