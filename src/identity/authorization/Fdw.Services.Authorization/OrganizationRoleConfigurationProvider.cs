using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the OrganizationRole configuration (FDW-1593).</summary>
/// <remarks>
/// An organization role's <c>Description</c> is not a column of <c>tenant.OrganizationRoleConfiguration</c> (FDW-6208): it is a row of the descriptions table under the description type
/// <c>OrganizationRole</c>, written in the unit of the role's save, filled once for every set of composed organization roles this provider reads, and removed with the role.
/// </remarks>
public sealed class OrganizationRoleConfigurationProvider
    : DomainConfigurationProviderBase<IOrganizationRoleImplementationConfiguration>,
      IOrganizationRoleConfigurationProvider,
      IConfigurationWriteObserver<IOrganizationRoleImplementationConfiguration>,
      IConfigurationReadObserver<IOrganizationRoleImplementationConfiguration>,
      IReferencedConfigurationProvider<IOrganizationRoleImplementationConfiguration>
{
    private const string DescriptionType = OrganizationRoleDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="OrganizationRoleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each organization role's description in the unit of the role's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the organization roles a read has just composed, once for the set (FDW-6208).</param>
    public OrganizationRoleConfigurationProvider(
        ILogger<OrganizationRoleConfigurationProvider> logger,
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
    /// <remarks>A organization role saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(IOrganizationRoleImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A organization role with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<IOrganizationRoleImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, o => o.Id, (o, text) => o.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>An organization role's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
