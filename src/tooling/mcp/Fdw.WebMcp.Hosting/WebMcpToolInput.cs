namespace Fdw.WebMcp.Hosting;

/// <summary>
/// One value a tool asks an agent for: the request property it binds to and how JSON Schema
/// describes it.
/// </summary>
/// <param name="Name">The request property's own name and casing, which is the key the agent sends.</param>
/// <param name="JsonType">The JSON Schema type.</param>
/// <param name="Format">The JSON Schema format, or empty when the type needs none.</param>
public sealed record WebMcpToolInput(string Name, string JsonType, string Format);
