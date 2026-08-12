using ModelContextProtocol.Server;

namespace Fdw.Mcp.Hosting.SampleTools;

/// <summary>
/// Stands in for the tool class a real tool package would ship. Nothing references it directly —
/// it reaches the server only through <see cref="SampleEchoToolType"/>.
/// </summary>
[McpServerToolType]
public sealed class SampleEchoTool
{
    /// <summary>Echoes its input, so the tool has a real invocable surface.</summary>
    /// <param name="text">Text to echo.</param>
    /// <returns>The supplied text.</returns>
    [McpServerTool(Name = "sample_echo")]
    public static string Echo(string text) => text;
}
