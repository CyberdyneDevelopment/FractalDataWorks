using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the category configuration.</summary>
/// <remarks>
/// A category's <c>Description</c> is not a column of <c>catalog.Category</c> (FDW-6815): it is a row of the descriptions table under the description type
/// <c>CatalogCategory</c>, written in the unit of the category's save, filled once for every set of composed categories and once for every page of domain records this
/// provider reads (the category tree is one such page), and removed with the category.
/// </remarks>
public sealed class CategoryConfigurationProvider
    : DomainConfigurationProviderBase<ICategoryImplementationConfiguration, CategoryDomainConfiguration>,
      ICategoryConfigurationProvider,
      IConfigurationWriteObserver<ICategoryImplementationConfiguration>,
      IConfigurationReadObserver<ICategoryImplementationConfiguration>,
      IConfigurationDomainReadObserver<CategoryDomainConfiguration>,
      IReferencedConfigurationProvider<ICategoryImplementationConfiguration>
{
    private const string DescriptionType = CategoryDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="CategoryConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each category's description in the unit of the category's save (FDW-6815).</param>
    /// <param name="reader">Reads the descriptions of the categories a read has just composed or paged, once for the set (FDW-6815).</param>
    public CategoryConfigurationProvider(
        ILogger<CategoryConfigurationProvider> logger,
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
    /// <remarks>A category saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(ICategoryImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A category with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<ICategoryImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, category => category.Id, (category, text) => category.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>The category tree reads one page of domain records, which the domain table answers without the text: it is filled here, once for the page.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<CategoryDomainConfiguration> records, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, category => category.Id, (category, text) => category.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A category's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
