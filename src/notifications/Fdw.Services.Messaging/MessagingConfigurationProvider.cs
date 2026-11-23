using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging;

/// <summary>Supplies the Messaging configuration.</summary>
/// <remarks>
/// A messaging configuration's <c>Description</c> is not a column of <c>msg.MessagingConfiguration</c> (FDW-7203, a group of FDW-6208): it is a row of the descriptions
/// table under the description type <c>Messaging</c>, written in the unit of the configuration's save, filled once for every set of composed configurations, and removed
/// with the configuration.
/// </remarks>
public sealed class MessagingConfigurationProvider
    : DomainConfigurationProviderBase<IMessagingImplementationConfiguration>,
      IMessagingConfigurationProvider,
      IConfigurationWriteObserver<IMessagingImplementationConfiguration>,
      IConfigurationReadObserver<IMessagingImplementationConfiguration>,
      IReferencedConfigurationProvider<IMessagingImplementationConfiguration>
{
    private const string DescriptionType = MessagingDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="MessagingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each configuration's description in the unit of its save (FDW-7203).</param>
    /// <param name="reader">Reads the descriptions of the configurations a read has just composed, once for the set (FDW-7203).</param>
    public MessagingConfigurationProvider(
        ILogger<MessagingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IDescriptionWriter writer,
        IDescriptionReader reader)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _writer = writer;
        _reader = reader;
    }

    /// <inheritdoc />
    /// <remarks>A configuration saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(IMessagingImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A configuration with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<IMessagingImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, messaging => messaging.Id, (messaging, text) => messaging.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A configuration's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
