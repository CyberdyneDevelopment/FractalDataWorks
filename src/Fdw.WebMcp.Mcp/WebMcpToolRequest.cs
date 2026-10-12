namespace Fdw.WebMcp.Mcp;

/// <summary>
/// The HTTP call one tool invocation makes, built from the tool's plan and an agent's arguments.
/// </summary>
/// <param name="Method">The HTTP verb.</param>
/// <param name="RelativeUrl">Path and query, relative to the host's own base address.</param>
/// <param name="Body">The JSON body, or <see langword="null"/> for a verb that carries none.</param>
/// <remarks>
/// Built as a value rather than as an <c>HttpRequestMessage</c> so that what an agent's arguments
/// turn into can be asserted without sending anything.
/// </remarks>
public sealed record WebMcpToolRequest(string Method, string RelativeUrl, string? Body);
