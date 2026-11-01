using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Scripts;
using Fdw.Services.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Replaces a stored SQL script (FDW-1442). The write policy is held by the DataEngineer role alone.</summary>
public abstract class UpdateScriptEndpointBase : CrudUpdateEndpointBase<ScriptPayload, ScriptPayload>
{
    private readonly ScriptConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="UpdateScriptEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored scripts.</param>
    protected UpdateScriptEndpointBase(ILogger<UpdateScriptEndpointBase> logger, ScriptConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "scripts";

    /// <inheritdoc />
    protected override string Route => "/scripts/{Name}";

    /// <inheritdoc />
    protected override HttpVerb UpdateVerb => HttpVerb.PUT;

    /// <inheritdoc />
    protected override string EndpointSummary => "Replace a stored SQL script";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Replaces the store, kind, text, description and parameters of a stored script with those in the body. The script is refused whole when it cannot be saved.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(ScriptPayload request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<ScriptPayload?>> FindForUpdate(ScriptPayload request, CancellationToken ct) =>
        ScriptRead.Find(_provider, request.Name, Logger, ct);

    /// <inheritdoc />
    protected override async Task<IGenericResult<ScriptPayload>> Update(ScriptPayload request, ScriptPayload existing, CancellationToken ct)
    {
        var original = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (original.IsFailure || original.Value is null)
        {
            return GenericResult<ScriptPayload>.Failure(ServicesResultCodes.ByName("ConfigurationNotFound"));
        }

        var script = ScriptPayloadReader.Read(request, original.Value.Id);
        if (script.IsFailure || script.Value is null)
        {
            return script.ToNewResult<ScriptPayload>();
        }

        return await ScriptSave.Update(_provider, script.Value, Logger, ct).ConfigureAwait(false);
    }
}
