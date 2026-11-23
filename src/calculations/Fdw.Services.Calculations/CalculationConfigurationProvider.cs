using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Commands;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations;

/// <summary>Supplies the CalculationEntity configuration.</summary>
/// <remarks>
/// A calculation's <c>Description</c> is not a column of <c>calc.CalculationEntity</c> (FDW-6208): it is a row of the descriptions table under the
/// description type <c>Calculation</c>, written in the unit of the calculation's save, filled once for every set of composed calculations and once for
/// every page of domain records this provider reads, and removed with the calculation.
/// </remarks>
public sealed class CalculationConfigurationProvider
    : DomainConfigurationProviderBase<ICalculationEntityImplementationConfiguration, CalculationEntityDomainConfiguration>,
      ICalculationConfigurationProvider,
      IPlacedConfigurationProvider<ICalculationEntityImplementationConfiguration>,
      IConfigurationWriteObserver<ICalculationEntityImplementationConfiguration>,
      IConfigurationReadObserver<ICalculationEntityImplementationConfiguration>,
      IConfigurationDomainReadObserver<CalculationEntityDomainConfiguration>,
      IReferencedConfigurationProvider<ICalculationEntityImplementationConfiguration>
{
    private const string DescriptionType = CalculationDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="CalculationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="placement">Places each member this provider makes, in the unit of work that makes it (FDW-3692).</param>
    /// <param name="writer">Writes each calculation's description in the unit of the calculation's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the calculations a read has just composed or paged, once for the set (FDW-6208).</param>
    public CalculationConfigurationProvider(
        ILogger<CalculationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IConfigurationPlacement placement,
        IDescriptionWriter writer,
        IDescriptionReader reader)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        Placement = placement;
        _writer = writer;
        _reader = reader;
    }

    /// <inheritdoc />
    public IConfigurationPlacement Placement { get; }

    /// <inheritdoc />
    public string PlacementKind => "Calculation";

    /// <inheritdoc />
    public bool Needs(ICalculationEntityImplementationConfiguration record) => true;

    /// <inheritdoc />
    /// <remarks>A calculation that carries no text removes the description it had; nothing is invented.</remarks>
    public Task<IGenericResult> Written(ICalculationEntityImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A calculation with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<ICalculationEntityImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, calculation => calculation.Id, (calculation, text) => calculation.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>The list of calculations is a page of domain records, which the domain table answers without the text: it is filled here, once for the page.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<CalculationEntityDomainConfiguration> records, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, calculation => calculation.Id, (calculation, text) => calculation.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A calculation's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
