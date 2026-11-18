using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>DELETE /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}</c> (datastores:write), FDW-6339. Removes a rule. It is kept, with its values and history, and can be restored.
/// Built by FDW-6489: it answers through the rule service.
/// </summary>
public abstract class RemoveDataOptionRuleEndpointBase : DataOptionRulePayloadEndpointBase<DataOptionRuleKeyRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RemoveDataOptionRuleEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected RemoveDataOptionRuleEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger, rules, names)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "RemoveDataOptionRule";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRulePayload>> Run(DataOptionRuleKeyRequest request, CancellationToken cancellationToken)
    {
        return await Payload(await Rules.Remove(DataOptionRuleMapping.Key(request), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "Remove a data option rule";
            s.Description = "Removes a rule. It is kept, with its values and history, and can be restored.";
        });
        ConfigureEndpoint();
    }
}
