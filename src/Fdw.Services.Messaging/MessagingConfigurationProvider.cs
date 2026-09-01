using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    : ServiceConfigurationProviderBase<
          MessagingConfiguration,
          IMessagingImplementationConfiguration,
          MessagingConfigurationCommand>,
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
    {
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IMessagingConfiguration>> GetHeader(
        string name, CancellationToken cancellationToken = default)
    {
        var header = await GetHeaderByName(name, cancellationToken).ConfigureAwait(false);
        return header.IsSuccess && header.Value is not null
            ? GenericResult<IMessagingConfiguration>.Success(header.Value)
            : header.ToNewResult<IMessagingConfiguration>();
    }

    /// <inheritdoc />
    protected override MessagingConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
