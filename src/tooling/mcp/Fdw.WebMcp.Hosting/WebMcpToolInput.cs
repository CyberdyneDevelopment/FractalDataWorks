using System.Collections.Generic;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// One value a tool asks an agent for: the request property it binds to and how JSON Schema
/// describes it.
/// </summary>
/// <param name="Name">The request property's own name and casing, which is the key the agent sends. Empty for the item of a list, which has no name of its own.</param>
/// <param name="JsonType">The JSON Schema type: a scalar's, or <c>array</c> or <c>object</c>.</param>
/// <param name="Format">The JSON Schema format, or empty when the type needs none.</param>
/// <param name="Required">Whether the member must be present. Only a member nested in an object, or a list or object member of the request, is ever required; a scalar of the request itself is not (see <see cref="WebMcpToolPlan"/>).</param>
/// <param name="Items">What each item of a list is, or <see langword="null"/> when this is not a list.</param>
/// <param name="Properties">The members of an object, in declaration order, or <see langword="null"/> when this is not an object.</param>
public sealed record WebMcpToolInput(
    string Name,
    string JsonType,
    string Format,
    bool Required = false,
    WebMcpToolInput? Items = null,
    IReadOnlyList<WebMcpToolInput>? Properties = null);
