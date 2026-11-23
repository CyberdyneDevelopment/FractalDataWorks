using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Transformations;

/// <summary>Supplies the Transformation domain configuration (<c>transform.Transformation</c>).</summary>
/// <remarks>
/// A transformation's <c>Description</c>, whatever its kind, is not a column of any of the seven kinds' tables (FDW-6208): it is a row of the descriptions table under the
/// description type <c>Transformation</c>, written in the unit of the transformation's save, filled once for every set of composed transformations this provider reads,
/// and removed with the transformation.
/// </remarks>
public sealed class TransformationConfigurationProvider
    : DomainConfigurationProviderBase<ITransformationImplementationConfiguration>,
      ITransformationConfigurationProvider,
      IConfigurationWriteObserver<ITransformationImplementationConfiguration>,
      IConfigurationReadObserver<ITransformationImplementationConfiguration>,
      IReferencedConfigurationProvider<ITransformationImplementationConfiguration>
{
    private const string DescriptionType = TransformationDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="TransformationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each transformation's description in the unit of the transformation's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the transformations a read has just composed, once for the set (FDW-6208).</param>
    public TransformationConfigurationProvider(
        ILogger<TransformationConfigurationProvider> logger,
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
    /// <remarks>A transformation saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(ITransformationImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A transformation with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<ITransformationImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, transformation => transformation.Id, (transformation, text) => transformation.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A transformation's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
