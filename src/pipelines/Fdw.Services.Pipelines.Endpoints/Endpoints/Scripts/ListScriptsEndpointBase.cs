using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Fdw.Services.Pipelines.Scripts;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Lists the stored SQL scripts, a page at a time (FDW-1442).</summary>
public abstract class ListScriptsEndpointBase : CrudPageEndpointBase<DomainConfiguration, ScriptSummaryPayload>
{
    private readonly ScriptConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="ListScriptsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored scripts.</param>
    protected ListScriptsEndpointBase(ILogger<ListScriptsEndpointBase> logger, ScriptConfigurationProvider provider) : base(logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    protected override string ResourceName => "scripts";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> LoadPage(PaginatedListRequest paging, CancellationToken ct) =>
        _provider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct);

    /// <inheritdoc />
    protected override ValueTask<bool> Include(DomainConfiguration item, CancellationToken ct) =>
        ValueTask.FromResult(!string.IsNullOrWhiteSpace(item.Name));

    /// <inheritdoc />
    protected override async Task<IGenericResult<ScriptSummaryPayload>> ToSummary(DomainConfiguration item, CancellationToken ct)
    {
        var found = await ScriptRead.Find(_provider, item.Name, Logger, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.ToNewResult<ScriptSummaryPayload>();
        }

        if (found.Value is not { } script)
        {
            return GenericResult<ScriptSummaryPayload>.Failure(ScriptEndpointLog.SavedButUnreadable(Logger, item.Name));
        }

        return GenericResult<ScriptSummaryPayload>.Success(new ScriptSummaryPayload
        {
            Name = script.Name, Description = script.Description, Connection = script.Connection, Kind = script.Kind,
        });
    }
}
