using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /data-option-tree/read</c> (datastores:read), FDW-6339. Answers every level from the DataConfigurationRules root down, to any depth, with what each leaf takes, and the value kinds, owner kinds and
/// custodian states a rule chooses from. Built by FDW-6342: it reads the registered collections, as the flat data option types route does, so there is no service and nothing to register.
/// </summary>
public abstract class GetDataOptionTreeEndpointBase : DataOptionRuleServiceEndpointBase<EmptyRequest, DataOptionTreePayload>
{
    /// <summary>Initializes a new instance of the <see cref="GetDataOptionTreeEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected GetDataOptionTreeEndpointBase(ILogger? logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override string RouteName => "GetDataOptionTree";

    /// <inheritdoc />
    protected override Task<IGenericResult<DataOptionTreePayload>> Run(EmptyRequest request, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult<DataOptionTreePayload>>(GenericResult<DataOptionTreePayload>.Success(DataOptionTreeMapping.Tree()));

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/data-option-tree/read");
        Policies("datastores:read");
        Summary(s =>
        {
            s.Summary = "Get the data option tree";
            s.Description = "Answers every level from the DataConfigurationRules root down, to any depth, with what each leaf takes, and the value kinds, owner kinds and custodian states a rule chooses from.";
        });
        ConfigureEndpoint();
    }
}
