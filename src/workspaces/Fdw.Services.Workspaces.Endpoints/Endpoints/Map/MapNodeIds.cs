using System;
using System.Globalization;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The id every map node carries: the kind of the object and its logical id, so an object has exactly one node
/// however it was reached and a client merges by id.
/// </summary>
/// <remarks>
/// Not the id of the workspace attachment that brought the object in: one object attached twice, or reached by an
/// explode as well as by the overview, is still one node.
/// </remarks>
internal static class MapNodeIds
{
    private const char Separator = ':';

    /// <summary>Id of a data set node.</summary>
    public static string DataSet(Guid id) => Format(MapKinds.DataSet, id);

    /// <summary>Id of a data store node.</summary>
    public static string DataStore(Guid id) => Format(MapKinds.DataStore, id);

    /// <summary>Id of a pipeline node.</summary>
    public static string Pipeline(Guid id) => Format(MapKinds.Pipeline, id);

    /// <summary>Id of a container node.</summary>
    public static string Container(Guid id) => Format(MapKinds.Container, id);

    /// <summary>Id of a data set field node.</summary>
    public static string DataSetField(Guid id) => Format(MapKinds.DataSetField, id);

    /// <summary>Id of a node for a resource of a kind this map does not draw itself, as its workspace holds it.</summary>
    public static string Other(string kind, Guid id) => Format(kind, id);

    /// <summary>Reads a node id back into the kind and logical id it was built from.</summary>
    /// <param name="nodeId">The id a client sends.</param>
    /// <param name="kind">The kind, when the id is one the map issues.</param>
    /// <param name="id">The logical id, when the id is one the map issues.</param>
    /// <returns><see langword="true"/> when the id names a data set, data store, pipeline or container.</returns>
    public static bool TryParse(string? nodeId, out string kind, out Guid id)
    {
        kind = string.Empty;
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(nodeId)) return false;

        var split = nodeId.IndexOf(Separator, StringComparison.Ordinal);
        if (split < 1) return false;
        if (!Guid.TryParseExact(nodeId[(split + 1)..], "D", out id)) return false;

        kind = nodeId[..split];
        return string.Equals(kind, MapKinds.DataSet, StringComparison.Ordinal)
            || string.Equals(kind, MapKinds.DataStore, StringComparison.Ordinal)
            || string.Equals(kind, MapKinds.Pipeline, StringComparison.Ordinal)
            || string.Equals(kind, MapKinds.Container, StringComparison.Ordinal);
    }

    private static string Format(string kind, Guid id)
        => string.Create(CultureInfo.InvariantCulture, $"{kind}{Separator}{id:D}");
}
