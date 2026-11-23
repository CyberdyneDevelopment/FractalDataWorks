using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>Supplies the Connection configuration.</summary>
/// <remarks>
/// A connection's <c>Description</c> is not a column of <c>conn.Connection</c> (FDW-6208): it is a row of the descriptions table under the description type <c>Connection</c>,
/// written in the unit of the connection's save, filled once for every set of composed connections this provider reads, and removed with the connection. The member is on
/// <see cref="IConnectionImplementationConfiguration"/>, so one observer serves every connection type.
/// </remarks>
public sealed class ConnectionConfigurationProvider
    : DomainConfigurationProviderBase<IConnectionImplementationConfiguration>,
      IConnectionConfigurationProvider,
      IConfigurationWriteObserver<IConnectionImplementationConfiguration>,
      IConfigurationReadObserver<IConnectionImplementationConfiguration>,
      IReferencedConfigurationProvider<IConnectionImplementationConfiguration>
{
    private const string DescriptionType = ConnectionDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="ConnectionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each connection's description in the unit of the connection's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the connections a read has just composed, once for the set (FDW-6208).</param>
    public ConnectionConfigurationProvider(
        ILogger<ConnectionConfigurationProvider> logger,
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
    /// <remarks>A connection saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(IConnectionImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A connection with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<IConnectionImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, connection => connection.Id, (connection, text) => connection.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A connection's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
