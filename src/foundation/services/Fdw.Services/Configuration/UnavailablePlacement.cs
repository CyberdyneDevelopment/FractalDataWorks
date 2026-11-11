using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// The placement of a host that registers none: it places nothing and refuses every member it is asked to place (FDW-3692).
/// </summary>
/// <remarks>
/// A provider that declares its members are placed cannot make one that sits nowhere. A host that never wired a placement in therefore
/// starts, reads and runs as before, and fails the first create with a message that names what is missing, in the same way a host with no
/// workspace permissions service refuses to create a workspace. It never places the member somewhere it guessed.
/// </remarks>
public sealed class UnavailablePlacement : IConfigurationPlacement
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UnavailablePlacement"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public UnavailablePlacement(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult> Place(PlacementRequest request, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenericResult.Failure(DefaultConfigurationProviderLog.PlacementUnavailable(_logger, request.Kind, request.Name)));
    }
}
