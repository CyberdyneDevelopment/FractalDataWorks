using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Looks up the transformations the mappings of a graph's components name (FDW-1661, FDW-2000), the way a pipeline save looks up the ones its
/// stages name.
/// </summary>
/// <remarks>
/// Why one lookup: <see cref="StageChainLookup"/> resolves a chain's names to ids and says what is wrong with a chain. A component input states its
/// mappings, and so its chains, in the setting its catalogue marks as taking conversions, so its mappings go through the same lookup and the
/// reader stores the ids it found. A name that is not found stays out, and the reader refuses it naming the mapping and the step.
/// </remarks>
public static class GraphChainLookup
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Finds the id of each transformation named by a mapping of a component of the graph.</summary>
    /// <param name="lookup">The chain lookup.</param>
    /// <param name="payload">The graph as the API states it.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The id of each transformation found, by name.</returns>
    public static async Task<IReadOnlyDictionary<string, Guid>> Resolve(StageChainLookup lookup, GraphPayload payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(payload);
        var maps = payload.Components.SelectMany(Maps).ToList();
        if (maps.Count == 0)
        {
            return StageChains.None.Ids;
        }

        var chains = await lookup.Resolve(payload.Name, null, maps, cancellationToken).ConfigureAwait(false);
        return chains.Ids;
    }

    // The chains the component's mappings state, from every setting its kind marks as taking conversions. It reads only what the lookup needs; a
    // mapping that is not shaped as the reader expects is refused by the reader, naming the component and the setting.
    private static IEnumerable<MapChain> Maps(GraphComponentPayload component)
    {
        // A kind the catalogue does not list states no chain here; the reader refuses it, naming the component.
        var kind = ComponentTypes.ByName(component.Kind);
        if (ReferenceEquals(kind, ComponentTypes.NotFound))
        {
            yield break;
        }

        foreach (var name in kind.Settings.Where(member => member.TakesConversions).Select(member => member.Name))
        {
            var stated = component.Settings.FirstOrDefault(setting => string.Equals(setting.Key, name, StringComparison.OrdinalIgnoreCase));
            if (stated.Value is null)
            {
                continue;
            }

            var element = stated.Value is JsonElement json ? json : JsonSerializer.SerializeToElement(stated.Value, JsonOptions);
            if (element.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var index = 0;
            foreach (var mapping in element.EnumerateArray())
            {
                if (Chain(mapping) is { Count: > 0 } chain)
                {
                    yield return new MapChain(component.Name, index, Text(mapping, "target"), chain);
                }

                index++;
            }
        }
    }

    private static List<PipelineFieldTransformationReference>? Chain(JsonElement mapping) =>
        mapping.ValueKind == JsonValueKind.Object
        && mapping.TryGetProperty("transformations", out var chain)
        && chain.ValueKind == JsonValueKind.Array
            ? [.. chain.EnumerateArray().Where(step => step.ValueKind == JsonValueKind.Object).Select(Step)]
            : null;

    private static PipelineFieldTransformationReference Step(JsonElement step) => new()
    {
        Transformation = step.TryGetProperty("transformation", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() : null,
        TransformationId = step.TryGetProperty("transformationId", out var id) && id.ValueKind == JsonValueKind.String && Guid.TryParse(id.GetString(), out var parsed) ? parsed : null,
        Ordinal = step.TryGetProperty("ordinal", out var ordinal) && ordinal.TryGetInt32(out var number) ? number : 0,
    };

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { } text ? text : string.Empty;
}
