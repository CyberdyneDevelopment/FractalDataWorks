using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>DELETE /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/values</c> (datastores:write), FDW-6339. Removes values of a rule, all or none. They are kept.
/// Built by FDW-6489: it answers through the rule service.
/// </summary>
public abstract class RemoveDataOptionValuesEndpointBase : DataOptionRulePayloadEndpointBase<RemoveDataOptionValuesRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RemoveDataOptionValuesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected RemoveDataOptionValuesEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger, rules, names)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "RemoveDataOptionValues";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRulePayload>> Run(RemoveDataOptionValuesRequest request, CancellationToken cancellationToken)
    {
        return await Payload(await Rules.RemoveValues(DataOptionRuleMapping.Key(request), request.Id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/values");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "Remove values of a data option rule";
            s.Description = "Removes values of a rule, all or none. They are kept.";
        });
        ConfigureEndpoint();
    }
}
