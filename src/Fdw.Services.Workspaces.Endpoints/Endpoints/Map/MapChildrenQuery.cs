using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>An explode request that passed validation: which kinds of child, which of them match, and which page of those.</summary>
/// <param name="Skip">How many matching children precede the page.</param>
/// <param name="Take">How many children the page holds at most, or nothing for every remaining one.</param>
/// <param name="Text">The text a child's name must contain, or nothing.</param>
/// <param name="Containers">Whether the node's containers are sent.</param>
/// <param name="DataSetFields">Whether a data set's fields are sent.</param>
/// <param name="Paged">Whether the request asked for a page or a search, and so wants the page described.</param>
internal sealed record MapChildrenQuery(int Skip, int? Take, string? Text, bool Containers, bool DataSetFields, bool Paged)
{
    private const string ContainerKind = MapKinds.Container;
    private const string FieldKind = MapKinds.DataSetField;

    /// <summary>Checks an explode request against the kind of node it names.</summary>
    /// <param name="request">The request.</param>
    /// <param name="nodeKind">The kind of node being exploded, from its id.</param>
    /// <param name="logger">Records a refusal.</param>
    public static IGenericResult<MapChildrenQuery> Parse(MapChildrenRequest request, string nodeKind, ILogger logger)
    {
        if (request.Skip is < 0)
            return MapRequestRefusals.Invalid<MapChildrenQuery>(logger, $"skip {request.Skip} is below 0");
        if (request.Take is < 1)
            return MapRequestRefusals.Invalid<MapChildrenQuery>(logger, $"take {request.Take} is below 1");

        var kinds = Kinds(request.Kinds);
        if (kinds is null)
            return MapRequestRefusals.Invalid<MapChildrenQuery>(logger, $"kinds '{request.Kinds}' names a kind of child that is not offered; the kinds are {ContainerKind}, {FieldKind}");

        var fields = kinds.Contains(FieldKind);
        if (fields && !string.Equals(nodeKind, MapKinds.DataSet, StringComparison.Ordinal))
            return MapRequestRefusals.Invalid<MapChildrenQuery>(logger, $"kinds {FieldKind} applies to a data set; the node is a {nodeKind}");

        var text = string.IsNullOrWhiteSpace(request.Q) ? null : request.Q.Trim();
        return GenericResult<MapChildrenQuery>.Success(
            new MapChildrenQuery(
                request.Skip.HasValue ? request.Skip.Value : 0,
                request.Take,
                text,
                kinds.Contains(ContainerKind),
                fields,
                request.Skip.HasValue || request.Take.HasValue || text is not null));
    }

    /// <summary>Keeps the children whose name holds the text, in name order.</summary>
    /// <param name="children">Every child of the kinds asked for.</param>
    public List<MapChild> Match(IEnumerable<MapChild> children)
        => children
            .Where(child => Text is null || child.Node.Label.Contains(Text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(child => child.Node.Kind, StringComparer.Ordinal)
            .ThenBy(child => child.Node.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(child => child.Node.Id, StringComparer.Ordinal)
            .ToList();

    /// <summary>Takes the page out of the matching children.</summary>
    /// <param name="matching">The children <see cref="Match"/> kept.</param>
    public List<MapChild> Slice(List<MapChild> matching)
    {
        var rest = matching.Skip(Skip);
        return (Take.HasValue ? rest.Take(Take.Value) : rest).ToList();
    }

    /// <summary>Describes the page that was sent, or nothing when the request did not ask for a page.</summary>
    /// <param name="matching">The children <see cref="Match"/> kept.</param>
    /// <param name="sent">The page <see cref="Slice"/> took.</param>
    public MapPageDto? Describe(List<MapChild> matching, List<MapChild> sent)
        => Paged
            ? new MapPageDto { Skip = Skip, Take = Take, HasMore = Skip + sent.Count < matching.Count, Total = matching.Count }
            : null;

    // Container when nothing is named; null when a name is not a kind of child.
    private static HashSet<string>? Kinds(string? named)
    {
        if (string.IsNullOrWhiteSpace(named)) return [ContainerKind];

        var kinds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in named.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(token, ContainerKind, StringComparison.OrdinalIgnoreCase)) kinds.Add(ContainerKind);
            else if (string.Equals(token, FieldKind, StringComparison.OrdinalIgnoreCase)) kinds.Add(FieldKind);
            else return null;
        }

        return kinds.Count == 0 ? null : kinds;
    }
}
