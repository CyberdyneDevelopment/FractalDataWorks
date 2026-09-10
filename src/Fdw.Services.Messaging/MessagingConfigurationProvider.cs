using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Messaging.Logging;

namespace Fdw.Services.Messaging;

/// <summary>
/// Reads the configured messaging services from <c>msg.Messaging</c>.
/// </summary>
/// <remarks>
/// Behaviourally the base; it exists so the domain's own
/// <see cref="IMessagingConfigurationProvider"/> has a concrete type behind it, which is what lets
/// <c>MessageService</c> and <c>AccessRequestService</c> name the domain they read rather than a
/// closed generic that two domains could satisfy.
/// </remarks>
public sealed class MessagingConfigurationProvider
    : ImplementationConfigurationProviderBase<MessagingConfiguration, IMessagingImplementationConfiguration, MessagingConfigurationCommand>,
      IMessagingConfigurationProvider
{

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration connection.</param>
    /// <param name="dataStoreName">The connection the domain's configuration rows live in.</param>
    /// <param name="pathName">The path those rows live under.</param>
    public MessagingConfigurationProvider(
        ILogger<MessagingConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "msg")
        : base(logger ?? NullLogger<MessagingConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
        => _log = logger ?? NullLogger<MessagingConfigurationProvider>.Instance;

    // The bases hold their loggers privately, so GetHeader keeps its own reference rather than
    // reaching for one it cannot see.
    private readonly ILogger _log;

    /// <inheritdoc />
    public async Task<IGenericResult<IMessagingConfiguration>> GetHeader(
        string name, CancellationToken cancellationToken = default)
    {
        var header = await GetByName(name, cancellationToken).ConfigureAwait(false);
        if (!header.IsSuccess)
            return header.ToNewResult<IMessagingConfiguration>();

        // Same split as the callers': a successful read that found no row is not a failed read, and
        // converting it as one throws instead of reporting which row is missing.
        return header.Value is not null
            ? GenericResult<IMessagingConfiguration>.Success(header.Value)
            : GenericResult<IMessagingConfiguration>.Failure(
                MessagingLog.LocationNotConfigured(_log, $"no Messaging row named '{name}' exists"));
    }
}
