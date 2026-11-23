using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations;

/// <summary>The provider of the versions of calculations held at organizations and workspaces (FDW-2560).</summary>
/// <remarks>
/// A version's <c>Description</c>, what the calculation is in the words of the place that holds the version, is not a column of
/// <c>calc.CalculationVersion</c> (FDW-6208): it is a row of the descriptions table under the description type <c>CalculationVersion</c>, written in the unit
/// of the version's save, filled once for every set of composed versions and once for every page of domain records this provider reads, and removed with
/// the version.
/// </remarks>
public sealed class CalculationVersionConfigurationProvider
    : DomainConfigurationProviderBase<ICalculationVersionImplementationConfiguration, CalculationVersionDomainConfiguration>,
      ICalculationVersionConfigurationProvider,
      IConfigurationWriteObserver<ICalculationVersionImplementationConfiguration>,
      IConfigurationReadObserver<ICalculationVersionImplementationConfiguration>,
      IConfigurationDomainReadObserver<CalculationVersionDomainConfiguration>,
      IReferencedConfigurationProvider<ICalculationVersionImplementationConfiguration>
{
    private const string DescriptionType = CalculationVersionDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="CalculationVersionConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">Reaches the data store.</param>
    /// <param name="dataStoreName">The data store the rows are held in.</param>
    /// <param name="pathName">The path of the container.</param>
    /// <param name="containerName">The container of the domain rows.</param>
    /// <param name="writer">Writes each version's description in the unit of the version's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the versions a read has just composed or paged, once for the set (FDW-6208).</param>
    public CalculationVersionConfigurationProvider(
        ILogger<CalculationVersionConfigurationProvider> logger,
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
    /// <remarks>The save route refuses a version with no text, so a version always writes one; an empty text would remove it, never invent one.</remarks>
    public Task<IGenericResult> Written(ICalculationVersionImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A version with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<ICalculationVersionImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, version => version.Id, (version, text) => version.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>The versions list reads pages of domain records, which the domain table answers without the text: it is filled here, once for the page.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<CalculationVersionDomainConfiguration> records, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, version => version.Id, (version, text) => version.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A version's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
