using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Tests.Search;

/// <summary>The descriptions table as a search of a container whose description is kept outside its table sees it (FDW-6208): the text of each row by its Id.</summary>
internal sealed class DescriptionsOfRows : IRowDescriptions
{
    private readonly Dictionary<Guid, string> _texts;

    public DescriptionsOfRows(IEnumerable<KeyValuePair<Guid, string>> texts) => _texts = texts.ToDictionary(pair => pair.Key, pair => pair.Value);

    /// <summary>The Id a test gives the row with a key, so a row and its description meet.</summary>
    public static Guid IdOf(int key) => new(key, 0, 0, [0, 0, 0, 0, 0, 0, 0, 1]);

    public int Asked { get; private set; }

    public Task<IGenericResult<IReadOnlyCollection<Guid>>> RowsDescribedBy(string text, CancellationToken cancellationToken = default)
    {
        Asked++;
        return Task.FromResult(GenericResult<IReadOnlyCollection<Guid>>.Success(
            _texts.Where(pair => pair.Value.Contains(text, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key).ToList()));
    }

    public Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> TextsOf(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<IReadOnlyDictionary<Guid, string>>.Success(
            _texts.Where(pair => ids.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value)));
}
