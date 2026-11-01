using System.Collections.Generic;
using System.Text.Json;
using Fdw.Services.Etl.Graphs.Tools;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The settings of a stage tool, read from the stage as the API states it (FDW-1396).</summary>
/// <remarks>
/// Why the stage's API shape: a tool's settings are the members of its stage, stated as the stage states them. Writing a graph back
/// takes them from that shape, so what is written back is what was read.
/// </remarks>
internal static class ToolSettings
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Takes the members a tool owns from a stage.</summary>
    /// <param name="tool">The tool.</param>
    /// <param name="stage">The stage as the API states it.</param>
    /// <returns>Each member the stage states, by the name the tool gives it.</returns>
    internal static IEnumerable<KeyValuePair<string, object?>> Members(StageToolComponentTypeBase tool, PipelineTransformDto stage)
    {
        var stated = JsonSerializer.SerializeToElement(stage, WebOptions);
        foreach (var member in tool.MemberNames)
        {
            if (stated.TryGetProperty(member, out var value) && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
            {
                yield return new KeyValuePair<string, object?>(member, value.Clone());
            }
        }
    }
}
