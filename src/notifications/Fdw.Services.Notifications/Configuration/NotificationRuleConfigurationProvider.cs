using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>Supplies the configured NotificationRule members.</summary>
/// <remarks>
/// A rule's <c>Description</c> is not a column of <c>notify.NotificationRule</c> (FDW-7203, a group of FDW-6208): it is a row of the descriptions table under the
/// description type <c>NotificationRule</c>, written in the unit of the rule's save, filled once for every set of composed rules and once for every page of domain
/// records this provider reads, and removed with the rule.
/// </remarks>
public sealed class NotificationRuleConfigurationProvider
    : DomainConfigurationProviderBase<INotificationRuleImplementationConfiguration, NotificationRuleDomainConfiguration>,
      INotificationRuleConfigurationProvider,
      IConfigurationWriteObserver<INotificationRuleImplementationConfiguration>,
      IConfigurationReadObserver<INotificationRuleImplementationConfiguration>,
      IConfigurationDomainReadObserver<NotificationRuleDomainConfiguration>,
      IReferencedConfigurationProvider<INotificationRuleImplementationConfiguration>
{
    private const string DescriptionType = NotificationRuleDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="NotificationRuleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each rule's description in the unit of the rule's save (FDW-7203).</param>
    /// <param name="reader">Reads the descriptions of the rules a read has just composed or paged, once for the set (FDW-7203).</param>
    public NotificationRuleConfigurationProvider(
        ILogger<NotificationRuleConfigurationProvider> logger,
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
    /// <remarks>A rule saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(INotificationRuleImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A rule with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<INotificationRuleImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, rule => rule.Id, (rule, text) => rule.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>The rules list and query read pages of domain records, which the domain table answers without the text: it is filled here, once for the page.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<NotificationRuleDomainConfiguration> records, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, rule => rule.Id, (rule, text) => rule.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A rule's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
