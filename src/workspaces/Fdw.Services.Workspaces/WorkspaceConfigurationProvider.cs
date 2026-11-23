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

namespace Fdw.Services.Workspaces;

/// <summary>Supplies the Workspace configuration.</summary>
/// <remarks>
/// A workspace's <c>Description</c> is not a column of <c>workspace.WorkspaceConfiguration</c> (FDW-6208): it is a row of the descriptions table under the
/// description type <c>Workspace</c>, written in the unit of the workspace's save, filled once for every set this provider reads, and removed with the workspace.
/// </remarks>
public sealed class WorkspaceConfigurationProvider
    : DomainConfigurationProviderBase<IWorkspaceImplementationConfiguration>,
      IWorkspaceConfigurationProvider,
      IConfigurationWriteObserver<IWorkspaceImplementationConfiguration>,
      IConfigurationReadObserver<IWorkspaceImplementationConfiguration>,
      IReferencedConfigurationProvider<IWorkspaceImplementationConfiguration>
{
    private const string DescriptionType = WorkspaceDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each workspace's description in the unit of the workspace's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the workspaces a read has just composed, once for the set (FDW-6208).</param>
    public WorkspaceConfigurationProvider(
        ILogger<WorkspaceConfigurationProvider> logger,
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
    /// <remarks>A workspace that carries no text removes the description it had; nothing is invented.</remarks>
    public Task<IGenericResult> Written(IWorkspaceImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A workspace with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<IWorkspaceImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, workspace => workspace.Id, (workspace, text) => workspace.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A workspace's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
