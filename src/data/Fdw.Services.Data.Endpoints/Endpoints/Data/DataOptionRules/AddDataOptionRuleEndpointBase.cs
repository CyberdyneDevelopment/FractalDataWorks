using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules</c> (datastores:write), FDW-6339. Adds a rule with its values: what it is based on, its path, how it combines, its owner and its custodian. Nothing has a default.
/// Built by FDW-6489: it answers through the rule service.
/// </summary>
public abstract class AddDataOptionRuleEndpointBase : DataOptionRulePayloadEndpointBase<AddDataOptionRuleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="AddDataOptionRuleEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected AddDataOptionRuleEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger, rules, names)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "AddDataOptionRule";

    /// <inheritdoc />
    protected override int SuccessStatus => 201;

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionRulePayload>> Run(AddDataOptionRuleRequest request, CancellationToken cancellationToken)
    {
        if (request.Rule is not { } body)
            return Refused(DataOptionRulesEndpointLog.RuleBodyMissing(EndpointLogger, RouteName, "rule"));

        var matches = DataOptionRuleMapping.Matches(EndpointLogger, body, request);
        if (matches.IsFailure)
            return matches.ToNewResult<DataOptionRulePayload>();

        return await Payload(await Rules.Add(DataOptionRuleMapping.NewRule(request, body), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "Add a data option rule";
            s.Description = "Adds a rule with its values: what it is based on, its path, how it combines, its owner and its custodian. Nothing has a default.";
        });
        ConfigureEndpoint();
    }
}
