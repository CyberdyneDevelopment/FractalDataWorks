using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the Permission configuration.</summary>
/// <remarks>
/// A permission's <c>Description</c> is not a column of <c>authz.Permission</c> (FDW-6208): it is a row of the descriptions table under the description type <c>Permission</c>, written in the
/// unit of the permission's save, filled once for every set of composed permissions and once for every page of domain records this provider reads, and removed with the permission.
/// </remarks>
public sealed class PermissionConfigurationProvider
    : DomainConfigurationProviderBase<IPermissionImplementationConfiguration, PermissionDomainConfiguration>,
      IPermissionConfigurationProvider,
      IConfigurationWriteObserver<IPermissionImplementationConfiguration>,
      IConfigurationReadObserver<IPermissionImplementationConfiguration>,
      IConfigurationDomainReadObserver<PermissionDomainConfiguration>,
      IReferencedConfigurationProvider<IPermissionImplementationConfiguration>
{
    private const string DescriptionType = PermissionDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="PermissionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each permission's description in the unit of the permission's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the permissions a read has just composed, once for the set or the page (FDW-6208).</param>
    public PermissionConfigurationProvider(
        ILogger<PermissionConfigurationProvider> logger,
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
    /// <remarks>A permission saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(IPermissionImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A permission with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<IPermissionImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, p => p.Id, (p, text) => p.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>The permission lists and queries read one page of domain records, which the domain table answers without the text: it is filled here, once for the page.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<PermissionDomainConfiguration> records, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, p => p.Id, (p, text) => p.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A permission's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
