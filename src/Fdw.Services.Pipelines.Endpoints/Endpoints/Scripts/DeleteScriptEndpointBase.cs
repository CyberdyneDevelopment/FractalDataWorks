using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Scripts;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Deletes a stored SQL script (FDW-1442).</summary>
public abstract class DeleteScriptEndpointBase : CrudDeleteEndpointBase<ScriptNameRequest>
{
    private readonly ScriptConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="DeleteScriptEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored scripts.</param>
    protected DeleteScriptEndpointBase(ILogger<DeleteScriptEndpointBase> logger, ScriptConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "scripts";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(ScriptNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(ScriptNameRequest request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existing.IsSuccess && existing.Value != null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(ScriptNameRequest request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return existing.IsSuccess && existing.Value is { } found
            ? await _provider.Delete(found.Id, ct).ConfigureAwait(false)
            : GenericResult.Success();
    }
}
