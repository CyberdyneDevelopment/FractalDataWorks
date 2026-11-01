using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Scripts;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Reads a stored SQL script by name (FDW-1442).</summary>
public abstract class GetScriptEndpointBase : CrudGetEndpointBase<ScriptNameRequest, ScriptPayload>
{
    private readonly ScriptConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="GetScriptEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored scripts.</param>
    protected GetScriptEndpointBase(ILogger<GetScriptEndpointBase> logger, ScriptConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "scripts";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(ScriptNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<ScriptPayload?>> FindByIdentifier(ScriptNameRequest request, CancellationToken ct) =>
        ScriptRead.Find(_provider, request.Name, Logger, ct);
}
