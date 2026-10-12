using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Writes a map stream as newline-delimited JSON: one object per line, each carrying its line type in <c>t</c>, so a client draws while the rest is still being read.</summary>
/// <remarks>
/// <para>
/// The lines come in a fixed order. <c>scope</c> first, then every <c>node</c> as it is read (with a <c>progress</c> line
/// every <see cref="ProgressEvery"/> nodes), then every <c>edge</c> between nodes that were sent, then <c>end</c> last. A
/// stream that closes without an <c>end</c> line is incomplete, and a client must treat it as an error: the server aborts
/// the connection when a failure it cannot report arrives after the first byte, so a cut is how incompleteness reaches the client.
/// </para>
/// <para>
/// A node or edge line carries exactly the members of the node or edge object the JSON form carries, beside <c>t</c>. The
/// <c>end</c> line carries the counts, the problem that stopped the read (<c>error</c>, or null), and, when the caller
/// stated a cap on nodes and it was reached, <c>truncated</c> with the <c>cap</c>. What a cap left out was not read, so the
/// <c>end</c> line says <c>leftOutKnown: false</c> and gives no count of it.
/// </para>
/// </remarks>
internal static class MapNdjsonWriter
{
    /// <summary>The media type a client names in <c>Accept</c> to get the stream.</summary>
    public const string MediaType = "application/x-ndjson";

    /// <summary>How many nodes are written between two progress lines.</summary>
    public const int ProgressEvery = 100;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Gets whether the request asked for the stream form.</summary>
    /// <param name="request">The request.</param>
    public static bool Wanted(HttpRequest request)
        => request.Headers.Accept.ToString().Contains(MediaType, StringComparison.OrdinalIgnoreCase);

    /// <summary>Writes the stream as lines to the body, and flushes it.</summary>
    /// <param name="body">The response body.</param>
    /// <param name="stream">The stream to write: its nodes are enumerated to the end (or to the cap), then its edges.</param>
    /// <param name="error">Reads the problem that stopped the read, once the nodes and edges have been enumerated.</param>
    /// <param name="maxNodes">The most nodes to send, or nothing for no cap.</param>
    /// <param name="ct">The cancellation token.</param>
    public static async Task Write(Stream body, MapStream stream, Func<ProblemDetails?> error, int? maxNodes, CancellationToken ct)
    {
        await Line(body, "scope", new JsonObject { ["scope"] = ToNode(stream.Scope), ["maxNodes"] = maxNodes }, ct).ConfigureAwait(false);
        await body.FlushAsync(ct).ConfigureAwait(false);

        var sent = new HashSet<string>(StringComparer.Ordinal);
        var truncated = await Nodes(body, stream, sent, maxNodes, ct).ConfigureAwait(false);
        var edges = await Edges(body, stream, sent, ct).ConfigureAwait(false);

        var end = new JsonObject
        {
            ["nodeCount"] = sent.Count,
            ["edgeCount"] = edges,
            ["unresolved"] = stream.State.Unresolved,
            ["truncated"] = truncated,
            ["cap"] = truncated ? maxNodes : null,
            ["error"] = error() is { } problem ? ToNode(problem) : null,
        };
        if (truncated) end["leftOutKnown"] = false;
        if (stream.Page is { } page) end["page"] = ToNode(page);

        await Line(body, "end", end, ct).ConfigureAwait(false);
        await body.FlushAsync(ct).ConfigureAwait(false);
    }

    // True when the cap was reached with more nodes to come.
    private static async Task<bool> Nodes(Stream body, MapStream stream, HashSet<string> sent, int? maxNodes, CancellationToken ct)
    {
        await foreach (var node in stream.Nodes(ct).WithCancellation(ct).ConfigureAwait(false))
        {
            if (maxNodes is { } cap && sent.Count >= cap) return true;

            sent.Add(node.Id);
            await Line(body, "node", ToNode(node), ct).ConfigureAwait(false);
            if (sent.Count % ProgressEvery != 0) continue;

            await Line(body, "progress", new JsonObject { ["nodes"] = sent.Count, ["edges"] = 0 }, ct).ConfigureAwait(false);
            await body.FlushAsync(ct).ConfigureAwait(false);
        }

        return false;
    }

    // An edge goes out only when both its ends did: a cap can leave a node unsent that the stream had already recorded. The
    // one end that need not have been sent is the node an explode was asked about, which the client already holds.
    private static async Task<int> Edges(Stream body, MapStream stream, HashSet<string> sent, CancellationToken ct)
    {
        var written = 0;
        var parent = stream.Scope.ParentNodeId;
        foreach (var edge in stream.Edges())
        {
            if (!Held(edge.Source, sent, parent) || !Held(edge.Target, sent, parent)) continue;

            await Line(body, "edge", ToNode(edge), ct).ConfigureAwait(false);
            written++;
        }

        return written;
    }

    private static bool Held(string nodeId, HashSet<string> sent, string? parent)
        => sent.Contains(nodeId) || string.Equals(nodeId, parent, StringComparison.Ordinal);

    private static JsonNode? ToNode(object value) => JsonSerializer.SerializeToNode(value, value.GetType(), Options);

    // The line type first, then the members of the object the line carries.
    private static async Task Line(Stream body, string type, JsonNode? payload, CancellationToken ct)
    {
        var line = new JsonObject { ["t"] = type };
        if (payload is JsonObject members)
        {
            foreach (var member in members.ToList())
            {
                members.Remove(member.Key);
                line[member.Key] = member.Value;
            }
        }

        await body.WriteAsync(Encoding.UTF8.GetBytes(line.ToJsonString(Options) + "\n"), ct).ConfigureAwait(false);
    }
}
