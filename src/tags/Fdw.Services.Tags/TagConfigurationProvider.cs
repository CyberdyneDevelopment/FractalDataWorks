using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Tags.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tags;

/// <summary>Supplies the configured tags, read from <c>tag.Tag</c> through the configuration gateway.</summary>
/// <remarks>
/// A tag's <c>Description</c> is not a column of <c>tag.TagConfiguration</c> (FDW-6208): it is a row of the descriptions table under the description type
/// <c>Tag</c>, written in the unit of the tag's save, filled once for every set this provider reads, and removed with the tag.
/// </remarks>
public sealed class TagConfigurationProvider
    : DomainConfigurationProviderBase<ITagImplementationConfiguration, TagDomainConfiguration>,
      ITagConfigurationProvider,
      IConfigurationWriteObserver<ITagImplementationConfiguration>,
      IConfigurationReadObserver<ITagImplementationConfiguration>,
      IReferencedConfigurationProvider<ITagImplementationConfiguration>
{
    private const string DescriptionType = TagDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="containerName">The domain table.</param>
    /// <param name="writer">Writes each tag's description in the unit of the tag's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the tags a read has just composed, once for the set (FDW-6208).</param>
    public TagConfigurationProvider(
        ILogger<TagConfigurationProvider> logger,
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
    /// <remarks>A tag that carries no text removes the description it had; nothing is invented.</remarks>
    public Task<IGenericResult> Written(ITagImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A tag with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<ITagImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, tag => tag.Id, (tag, text) => tag.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A tag's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
