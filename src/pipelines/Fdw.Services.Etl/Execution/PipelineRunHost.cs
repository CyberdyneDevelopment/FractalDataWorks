using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Names the host that accepts a pipeline run (FDW-4084): the Id of this host's Host row, found by the host's application name through the hosts provider, as the pipeline event
/// broadcaster finds it. A host with no Host row has no identity, and the store neither accepts a run nor takes one up for it; no identity is assumed.
/// </summary>
internal static class PipelineRunHost
{
    /// <summary>Reads the Id of this host's Host row.</summary>
    /// <param name="services">The scope's services, which hold the hosts provider and the host environment.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Id, or a failure naming the host and why its row could not be used.</returns>
    public static async Task<IGenericResult<Guid>> IdOf(IServiceProvider services, ILogger logger, CancellationToken cancellationToken)
    {
        var name = services.GetRequiredService<IHostEnvironment>().ApplicationName;
        var host = await services.GetRequiredService<IHostConfigurationProvider>().Get(name, cancellationToken).ConfigureAwait(false);
        if (host.IsFailure)
        {
            return GenericResult<Guid>.Failure(PipelineRunRecoveryLog.HostRowUnavailable(logger, name, host.CurrentMessage));
        }

        return host.Value is { } row && row.Id != Guid.Empty
            ? GenericResult<Guid>.Success(row.Id)
            : GenericResult<Guid>.Failure(PipelineRunRecoveryLog.HostRowUnavailable(logger, name, "the Host row answered has no Id"));
    }
}
