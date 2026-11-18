using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/values</c> (datastores:write), FDW-6339. Adds values, literals or parameters, to a rule, all or none.
/// Built by FDW-6489: it answers through the rule service.
/// </summary>
public abstract class AddDataOptionValuesEndpointBase : DataOptionRulePayloadEndpointBase<AddDataOptionValuesRequest>
{
    /// <summary>Initializes a new instance of the <see cref="AddDataOptionValuesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected AddDataOptionValuesEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger, rules, names)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "AddDataOptionValues";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRulePayload>> Run(AddDataOptionValuesRequest request, CancellationToken cancellationToken)
    {
        if (request.Values is not { } body)
            return Refused(DataOptionRulesEndpointLog.RuleBodyMissing(EndpointLogger, RouteName, "values"));

        return await Payload(await Rules.AddValues(DataOptionRuleMapping.Key(request), DataOptionRuleMapping.NewValues(body), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/{RuleId}/values");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "Add values to a data option rule";
            s.Description = "Adds values, literals or parameters, to a rule, all or none.";
        });
        ConfigureEndpoint();
    }
}
