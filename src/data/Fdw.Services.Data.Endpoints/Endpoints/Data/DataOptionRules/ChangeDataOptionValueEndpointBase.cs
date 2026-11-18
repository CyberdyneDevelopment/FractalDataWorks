using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>PATCH /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/values/{ValueId}</c> (datastores:write), FDW-6339. Changes one value of a rule; a field left out keeps the stored one.
/// Built by FDW-6489: it answers through the rule service.
/// </summary>
public abstract class ChangeDataOptionValueEndpointBase : DataOptionRulePayloadEndpointBase<ChangeDataOptionValueRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ChangeDataOptionValueEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected ChangeDataOptionValueEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger, rules, names)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "ChangeDataOptionValue";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRulePayload>> Run(ChangeDataOptionValueRequest request, CancellationToken cancellationToken)
    {
        if (request.Change is not { } body)
            return Refused(DataOptionRulesEndpointLog.RuleBodyMissing(EndpointLogger, RouteName, "change"));

        return await Payload(await Rules.ChangeValue(DataOptionRuleMapping.ValueChange(request, body), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/values/{ValueId}");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "Change a value of a data option rule";
            s.Description = "Changes one value of a rule; a field left out keeps the stored one.";
        });
        ConfigureEndpoint();
    }
}
