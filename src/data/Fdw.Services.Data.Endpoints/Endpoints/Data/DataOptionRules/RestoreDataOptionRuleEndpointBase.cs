using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/restore</c> (datastores:write), FDW-6339. Restores a removed rule. It comes back switched off.
/// Built by FDW-6489: it answers through the rule service.
/// </summary>
public abstract class RestoreDataOptionRuleEndpointBase : DataOptionRulePayloadEndpointBase<DataOptionRuleKeyRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RestoreDataOptionRuleEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected RestoreDataOptionRuleEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger, rules, names)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "RestoreDataOptionRule";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRulePayload>> Run(DataOptionRuleKeyRequest request, CancellationToken cancellationToken)
    {
        return await Payload(await Rules.Restore(DataOptionRuleMapping.Key(request), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/restore");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "Restore a data option rule";
            s.Description = "Restores a removed rule. It comes back switched off.";
        });
        ConfigureEndpoint();
    }
}
