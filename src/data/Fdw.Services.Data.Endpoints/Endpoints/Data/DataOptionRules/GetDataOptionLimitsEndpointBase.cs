using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/limits/read</c> (datastores:read), FDW-6339. Answers the longest text each field of a rule and of a value takes, from the containers'
/// declared fields, and every changeable member of a rule and of a value. Built by FDW-6575: the rule service reads the declared lengths of the two rule tables (a field that declares none is left out, never given a
/// number) and the members come from the change catalog.
/// </summary>
public abstract class GetDataOptionLimitsEndpointBase : DataOptionRuleServiceEndpointBase<DataOptionContainerRequest, DataOptionLimitsPayload>
{
    private readonly IDataConfigurationRules _rules;

    /// <summary>Initializes a new instance of the <see cref="GetDataOptionLimitsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    protected GetDataOptionLimitsEndpointBase(ILogger? logger, IDataConfigurationRules rules)
        : base(logger)
    {
        _rules = rules;
    }

    /// <inheritdoc />
    protected override string RouteName => "GetDataOptionLimits";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataOptionLimitsPayload>> Run(DataOptionContainerRequest request, CancellationToken cancellationToken)
    {
        var limits = await _rules.Limits(cancellationToken).ConfigureAwait(false);
        return limits.IsSuccess && limits.Value is { } declared
            ? GenericResult<DataOptionLimitsPayload>.Success(new DataOptionLimitsPayload
            {
                Rule = declared.Rule.ToDictionary(length => length.Key, length => length.Value, System.StringComparer.Ordinal),
                Value = declared.Value.ToDictionary(length => length.Key, length => length.Value, System.StringComparer.Ordinal),
                Members = [.. DataConfigurationRuleCatalog.Members.Select(member => new DataOptionMemberPayload { Name = member.Name, DisplayName = member.DisplayName, Of = member.Of, Clearable = member.Clearable })],
            })
            : limits.ToNewResult<DataOptionLimitsPayload>();
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules/limits/read");
        Policies("datastores:read");
        Summary(s =>
        {
            s.Summary = "Get the data option rule limits";
            s.Description = "Answers the longest text each field of a rule and of a value takes, from the containers' declared fields, and every changeable member of a rule and of a value.";
        });
        ConfigureEndpoint();
    }
}
