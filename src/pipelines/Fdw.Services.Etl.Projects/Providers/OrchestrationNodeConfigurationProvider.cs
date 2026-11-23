using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Commands;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Providers;

/// <summary>Supplies the OrchestrationNode configuration.</summary>
/// <remarks>
/// An orchestration node's <c>Description</c> is not a column of its implementation table (FDW-7323, a group of FDW-6208): it is a row of the descriptions table under the
/// description type <c>OrchestrationNode</c>, written in the unit of the save, filled once for every set of composed members, and removed with the member.
/// The text is kept under the Id of the domain row, which the provider stamps onto the implementation.
/// </remarks>
public sealed class OrchestrationNodeConfigurationProvider
    : DomainConfigurationProviderBase<IOrchestrationNodeImplementationConfiguration>,
      IOrchestrationNodeConfigurationProvider,
      IConfigurationWriteObserver<IOrchestrationNodeImplementationConfiguration>,
      IConfigurationReadObserver<IOrchestrationNodeImplementationConfiguration>,
      IReferencedConfigurationProvider<IOrchestrationNodeImplementationConfiguration>
{
    private const string DescriptionType = OrchestrationNodeDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodeConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each description in the unit of its save (FDW-7323).</param>
    /// <param name="reader">Reads the descriptions of the members a read has just composed, once for the set (FDW-7323).</param>
    public OrchestrationNodeConfigurationProvider(
        ILogger<OrchestrationNodeConfigurationProvider> logger,
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
    /// <remarks>A member saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(IOrchestrationNodeImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A member with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<IOrchestrationNodeImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, member => member.Id, (member, text) => member.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A member's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
