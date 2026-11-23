using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the DataSetAnnotation configuration.</summary>
/// <remarks>
/// An annotation's <c>Description</c> and the description it gives each field are not columns of the catalog tables (FDW-6208): they are rows of the descriptions table under the
/// description types <c>DataSetAnnotation</c> and <c>DataSetAnnotationField</c> (owned by the annotation; the thing is the field description row's own Id, named by its field), written in the unit
/// of the annotation's save, filled once for every set this provider reads, and removed with the annotation.
/// </remarks>
public sealed class DataSetAnnotationConfigurationProvider
    : DomainConfigurationProviderBase<IDataSetAnnotationImplementationConfiguration>,
      IDataSetAnnotationConfigurationProvider,
      IConfigurationWriteObserver<IDataSetAnnotationImplementationConfiguration>,
      IConfigurationReadObserver<IDataSetAnnotationImplementationConfiguration>,
      IReferencedConfigurationProvider<IDataSetAnnotationImplementationConfiguration>
{
    private const string AnnotationType = DataSetAnnotationDescriptionType.OptionName;
    private const string FieldType = DataSetAnnotationFieldDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="DataSetAnnotationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each annotation's description, and each field's, in the unit of the annotation's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the annotations a read has just composed, once for the set (FDW-6208).</param>
    public DataSetAnnotationConfigurationProvider(
        ILogger<DataSetAnnotationConfigurationProvider> logger,
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
    /// <remarks>
    /// An annotation saved with no text removes the one it had; nothing invents one. Each field description is handed over by its own Id, with the field's name, owned by the annotation: a
    /// field whose text is blank loses its description row, and one the save no longer holds loses it too.
    /// </remarks>
    public async Task<IGenericResult> Written(IDataSetAnnotationImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var described = await _writer.Describe(AnnotationType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken).ConfigureAwait(false);
        return described.IsFailure
            ? described
            : await _writer.DescribeOwned(
                FieldType, DescriptionSubType, new DescriptionOwner(record.Id, record.Name),
                [.. record.FieldDescriptions.Select(field => new OwnedDescription(field.Id, field.FieldName, field.Description))], unit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>An annotation or a field with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public async Task<IGenericResult> Read(IReadOnlyList<IDataSetAnnotationImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
    {
        var annotations = await _reader.Fill(AnnotationType, DescriptionSubType, records, annotation => annotation.Id, (annotation, text) => annotation.Description = text, cancellationToken).ConfigureAwait(false);
        if (annotations.IsFailure)
            return annotations;

        // The field texts are read only for the annotations whose records hold field descriptions: a record-only read carries none.
        var owners = records.Where(annotation => annotation.FieldDescriptions.Count > 0).Select(annotation => annotation.Id).ToList();
        if (owners.Count == 0)
            return GenericResult.Success();

        var texts = await _reader.ReadOwnedBy(FieldType, DescriptionSubType, owners, cancellationToken).ConfigureAwait(false);
        if (texts.IsFailure || texts.Value is not { } held)
            return texts;

        foreach (var field in records.SelectMany(annotation => annotation.FieldDescriptions))
        {
            if (held.TryGetValue(field.Id, out var text))
                field.Description = text.Text;
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    /// <remarks>An annotation's descriptions and those of its fields name it, so they go with it, in every language, before it is retired.</remarks>
    public async Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var about = await _writer.DeleteAbout(AnnotationType, memberId, unit, cancellationToken).ConfigureAwait(false);
        return about.IsFailure
            ? about
            : await _writer.DeleteOwnedBy(FieldType, memberId, unit, cancellationToken).ConfigureAwait(false);
    }
}
