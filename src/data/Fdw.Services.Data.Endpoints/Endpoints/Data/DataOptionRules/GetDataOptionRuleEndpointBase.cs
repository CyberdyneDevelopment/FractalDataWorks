using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/read</c> (datastores:read), FDW-6339. Answers one data option rule with its values.
/// Built by FDW-6489: it answers through the rule service.
/// </summary>
public abstract class GetDataOptionRuleEndpointBase : DataOptionRulePayloadEndpointBase<DataOptionRuleKeyRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetDataOptionRuleEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected GetDataOptionRuleEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger, rules, names)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "GetDataOptionRule";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRulePayload>> Run(DataOptionRuleKeyRequest request, CancellationToken cancellationToken)
    {
        return await Payload(await Rules.Get(DataOptionRuleMapping.Key(request), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/read");
        Policies("datastores:read");
        Summary(s =>
        {
            s.Summary = "Get a data option rule";
            s.Description = "Answers one data option rule with its values.";
        });
        ConfigureEndpoint();
    }
}
