using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Scripts;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Creates a stored SQL script (FDW-1442). The write policy is held by the DataEngineer role alone.</summary>
public abstract class CreateScriptEndpointBase : CrudCreateEndpointBase<ScriptPayload, ScriptPayload>
{
    private readonly ScriptConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="CreateScriptEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored scripts.</param>
    protected CreateScriptEndpointBase(ILogger<CreateScriptEndpointBase> logger, ScriptConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "scripts";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a stored SQL script";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Stores a SQL script: the store it runs on, whether it is a query or a command, its text with named parameters, and the parameters it declares. The script is refused whole, naming each problem, when it cannot be saved.";

    /// <inheritdoc />
    protected override string GetResourceName(ScriptPayload request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(ScriptPayload request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existing.IsSuccess && existing.Value != null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<ScriptPayload>> Create(ScriptPayload request, CancellationToken ct)
    {
        var script = ScriptPayloadReader.Read(request, Guid.CreateVersion7());
        if (script.IsFailure || script.Value is null)
        {
            return script.ToNewResult<ScriptPayload>();
        }

        return await ScriptSave.Save(_provider, script.Value, Logger, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(ScriptPayload detail, CancellationToken ct) => Send.ResponseAsync(detail, 201, ct);
}
