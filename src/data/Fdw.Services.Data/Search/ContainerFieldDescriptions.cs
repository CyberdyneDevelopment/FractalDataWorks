using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Descriptions.Abstractions;

namespace Fdw.Services.Data.Search;

/// <summary>
/// The description of the fields of the containers of data stores, which is kept in the descriptions table under the description type <c>DataContainerField</c> and not in a column of
/// <c>data.DataContainerField</c> (FDW-6589): the search of the fields asks it which fields a text finds and what each is described by.
/// </summary>
internal sealed class ContainerFieldDescriptions : IRowDescriptions
{
    private const string FieldType = DataContainerFieldDescriptionType.OptionName;

    private readonly IDescriptionReader _reader;

    public ContainerFieldDescriptions(IDescriptionReader reader) => _reader = reader;

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyCollection<Guid>>> RowsDescribedBy(string text, CancellationToken cancellationToken = default)
        => _reader.Find(FieldType, DescriptionTextSubType.OptionName, text, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> TextsOf(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        var read = await _reader.ReadMany(FieldType, DescriptionTextSubType.OptionName, ids, cancellationToken).ConfigureAwait(false);
        return read.IsFailure
            ? read.ToNewResult<IReadOnlyDictionary<Guid, string>>()
            : GenericResult<IReadOnlyDictionary<Guid, string>>.Success(read.Value!.ToDictionary(entry => entry.Key, entry => entry.Value.Text));
    }
}
