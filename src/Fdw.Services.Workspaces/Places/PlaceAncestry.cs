using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The default <see cref="IPlaceAncestry"/>: the places above a place, found by asking the source of each kind of place which place is directly above
/// and following the answers up to the top (FDW-2538). The number of sources bounds the walk, since a place is passed once per kind at most.
/// </summary>
public sealed class PlaceAncestry : IPlaceAncestry
{
    private readonly Dictionary<string, IPlaceParentSource> _sources;
    private readonly ILogger<PlaceAncestry> _logger;

    /// <summary>Initializes a new instance of the <see cref="PlaceAncestry"/> class.</summary>
    /// <param name="sources">One source for each kind of place; two for one kind is refused.</param>
    /// <param name="logger">The logger.</param>
    public PlaceAncestry(IEnumerable<IPlaceParentSource> sources, ILogger<PlaceAncestry>? logger)
    {
        _sources = (sources ?? throw new ArgumentNullException(nameof(sources))).ToDictionary(source => source.Kind, StringComparer.Ordinal);
        _logger = logger ?? NullLogger<PlaceAncestry>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<PlaceStep>>> Get(string placeKind, Guid placeId, CancellationToken cancellationToken = default)
    {
        var place = string.Create(CultureInfo.InvariantCulture, $"{placeKind} '{placeId:D}'");
        var steps = new List<PlaceStep>();
        PlaceRef? current = new PlaceRef(placeKind, placeId);
        while (current is not null)
        {
            if (steps.Count >= _sources.Count)
                return GenericResult<IReadOnlyList<PlaceStep>>.Failure(PlaceAncestryLog.TreeBroken(_logger, place));

            if (!_sources.TryGetValue(current.Kind, out var source))
                return GenericResult<IReadOnlyList<PlaceStep>>.Failure(PlaceAncestryLog.KindUnknown(_logger, current.Kind));

            var above = await source.Above(current.Id, cancellationToken).ConfigureAwait(false);
            if (above.IsFailure)
                return above.ToNewResult<IReadOnlyList<PlaceStep>>();

            steps.Add(new PlaceStep(current.Kind, current.Id, steps.Count));
            current = above.Value;
        }

        PlaceAncestryLog.Read(_logger, place, steps.Count);
        return GenericResult<IReadOnlyList<PlaceStep>>.Success(steps);
    }
}
