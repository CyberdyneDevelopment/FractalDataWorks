using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.DataConfigurationRules;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /data-option-paths/check</c> (datastores:read), FDW-6339. Answers whether a path resolves at every level and ends at a leaf, and what the leaf takes, or the coded refusal that names the level.
/// Built by FDW-6566: it resolves the path with the check the rule service makes when a rule is added, so both refuse a path with the same codes; there is no service and nothing to register. A body
/// without a path or a method is refused with its own code before anything is resolved.
/// </summary>
public abstract class CheckDataOptionPathEndpointBase : DataOptionRuleServiceEndpointBase<CheckDataOptionPathRequest, DataOptionPathCheckPayload>
{
    /// <summary>Initializes a new instance of the <see cref="CheckDataOptionPathEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected CheckDataOptionPathEndpointBase(ILogger? logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "CheckDataOptionPath";

    /// <inheritdoc />
    protected override Task<IGenericResult<DataOptionPathCheckPayload>> Run(CheckDataOptionPathRequest request, CancellationToken cancellationToken)
        => Task.FromResult(Check(request));

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/data-option-paths/check");
        Policies("datastores:read");
        Summary(s =>
        {
            s.Summary = "Check a data option path";
            s.Description = "Answers whether a path resolves at every level and ends at a leaf, and what the leaf takes, or the coded refusal that names the level.";
        });
        ConfigureEndpoint();
    }

    private IGenericResult<DataOptionPathCheckPayload> Check(CheckDataOptionPathRequest request)
    {
        if (request.Path is not { } body)
            return GenericResult<DataOptionPathCheckPayload>.Failure(DataOptionRulesEndpointLog.RuleBodyMissing(EndpointLogger, RouteName, "path"));

        if (body.Path is not { Count: > 0 } path)
            return GenericResult<DataOptionPathCheckPayload>.Failure(DataOptionRulesEndpointLog.RuleFieldMissing(EndpointLogger, RouteName, "path"));

        if (string.IsNullOrWhiteSpace(body.Method))
            return GenericResult<DataOptionPathCheckPayload>.Failure(DataOptionRulesEndpointLog.RuleFieldMissing(EndpointLogger, RouteName, "method"));

        var resolved = DataOptionPathCheck.Resolve(EndpointLogger, path, body.Method);
        return resolved.IsSuccess && resolved.Value is { } kind
            ? GenericResult<DataOptionPathCheckPayload>.Success(new DataOptionPathCheckPayload
            {
                Path = [.. path],
                Method = body.Method,
                Family = path.ElementAt(1),
                MinValues = kind.MinValues,
                MaxValues = kind.MaxValues,
            })
            : resolved.ToNewResult<DataOptionPathCheckPayload>();
    }
}
