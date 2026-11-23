using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations;

/// <summary>Supplies the EscalationPolicy configuration.</summary>
/// <remarks>
/// Escalation policy's <c>Description</c> is not a column of its implementation table (FDW-7205, a group of FDW-6208): it is a row of the descriptions table under the
/// description type <c>EscalationPolicy</c>, written in the unit of the save, filled once for every set of composed members, and removed with the member. The text is kept under the
/// Id of the domain row, which the provider stamps onto the implementation.
/// </remarks>
public sealed class EscalationConfigurationProvider
    : DomainConfigurationProviderBase<IEscalationPolicyImplementationConfiguration>,
      IEscalationConfigurationProvider,
      IConfigurationWriteObserver<IEscalationPolicyImplementationConfiguration>,
      IConfigurationReadObserver<IEscalationPolicyImplementationConfiguration>,
      IReferencedConfigurationProvider<IEscalationPolicyImplementationConfiguration>
{
    private const string DescriptionType = EscalationPolicyDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="EscalationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each escalation policy's description in the unit of its save (FDW-7205).</param>
    /// <param name="reader">Reads the descriptions of the escalation policys a read has just composed, once for the set (FDW-7205).</param>
    public EscalationConfigurationProvider(
        ILogger<EscalationConfigurationProvider> logger,
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
    /// <remarks>A escalation policy saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(IEscalationPolicyImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A escalation policy with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<IEscalationPolicyImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, member => member.Id, (member, text) => member.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A escalation policy's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
