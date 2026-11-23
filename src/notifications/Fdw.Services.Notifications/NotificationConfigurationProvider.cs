using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Commands;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications;

/// <summary>Supplies the Notification configuration.</summary>
/// <remarks>
/// A notification's <c>Description</c> is not a column of <c>notify.Notification</c> (FDW-7203, a group of FDW-6208): it is a row of the descriptions table under the
/// description type <c>Notification</c>, written in the unit of the notification's save, filled once for every set of composed notifications and once for every page of
/// domain records this provider reads, and removed with the notification.
/// </remarks>
public sealed class NotificationConfigurationProvider
    : DomainConfigurationProviderBase<INotificationImplementationConfiguration, NotificationDomainConfiguration>,
      INotificationConfigurationProvider,
      IConfigurationWriteObserver<INotificationImplementationConfiguration>,
      IConfigurationReadObserver<INotificationImplementationConfiguration>,
      IConfigurationDomainReadObserver<NotificationDomainConfiguration>,
      IReferencedConfigurationProvider<INotificationImplementationConfiguration>
{
    private const string DescriptionType = NotificationDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="NotificationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each notification's description in the unit of the notification's save (FDW-7203).</param>
    /// <param name="reader">Reads the descriptions of the notifications a read has just composed or paged, once for the set (FDW-7203).</param>
    public NotificationConfigurationProvider(
        ILogger<NotificationConfigurationProvider> logger,
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
    /// <remarks>A notification saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(INotificationImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A notification with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<INotificationImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, notification => notification.Id, (notification, text) => notification.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>The notifications list reads pages of domain records, which the domain table answers without the text: it is filled here, once for the page.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<NotificationDomainConfiguration> records, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, notification => notification.Id, (notification, text) => notification.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A notification's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
