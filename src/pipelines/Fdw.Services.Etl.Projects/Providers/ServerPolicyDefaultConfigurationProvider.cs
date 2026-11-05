using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Providers;

/// <summary>Reads the ServerPolicyDefault domain rows and routes each to the implementation provider it names.</summary>
/// <remarks>
/// Why the consumers read through here when they need the defaults: they are configuration, served from the host's
/// server config like the data gateway's own row, so a missing row is a failed read with its message at the point of use,
/// not a value held from startup (FDW-3580).
/// </remarks>
public sealed class ServerPolicyDefaultConfigurationProvider
    : DomainConfigurationProviderBase<IServerPolicyDefaultImplementationConfiguration>,
      IServerPolicyDefaultConfigurationProvider
{
    private readonly ILogger<ServerPolicyDefaultConfigurationProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="ServerPolicyDefaultConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">The configuration gateway provider.</param>
    /// <param name="dataStoreName">The data store holding the domain table.</param>
    /// <param name="pathName">The path holding the domain table.</param>
    /// <param name="containerName">The domain table.</param>
    public ServerPolicyDefaultConfigurationProvider(
        ILogger<ServerPolicyDefaultConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _logger = logger ?? NullLogger<ServerPolicyDefaultConfigurationProvider>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IServerPolicyDefaultImplementationConfiguration>> GetCurrent(CancellationToken ct = default)
    {
        var rows = await Get(ct).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is not { } read)
        {
            return GenericResult<IServerPolicyDefaultImplementationConfiguration>.Failure(
                ProjectConfigurationLog.ServerPolicyDefaultsUnreadable(_logger, rows.NoValueReason().Message));
        }

        // Exactly one: which defaults the server runs on is the decision the row records, so two would make it depend on
        // read order and none leaves no baseline.
        return read.Count == 1
            ? GenericResult<IServerPolicyDefaultImplementationConfiguration>.Success(read[0])
            : GenericResult<IServerPolicyDefaultImplementationConfiguration>.Failure(
                ProjectConfigurationLog.ServerPolicyDefaultsMissing(_logger, read.Count));
    }
}
