using System.Collections.Generic;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// A catalogue that exercises every rule the transports must agree on: a filterable list, a path
/// parameter with a parent list to recover from, a body-carrying tool with a preset, a bodyless
/// delete, a tool with no request type, and a request property no schema type can describe.
/// </summary>
/// <remarks>
/// Shared by the golden-output test and the transport-parity tests. Both WebMCP and MCP are driven
/// from it, so a rule one of them honours and the other does not shows up as a failing test rather
/// than as an agent getting a different answer depending on how it connected.
/// </remarks>
internal static class WebMcpGoldenCatalogue
{
    internal sealed class ListItemsRequest
    {
        public string? ReferenceId { get; set; }

        public int? Take { get; set; }

        public System.DateTimeOffset? Since { get; set; }
    }

    internal sealed class ItemNameRequest
    {
        public string? Name { get; set; }
    }

    internal sealed class CreateItemRequest
    {
        public string? Name { get; set; }

        public string? ServiceType { get; set; }

        public bool Enabled { get; set; }

        public System.Guid? OwnerId { get; set; }

        // No JSON Schema primitive describes this; it must be skipped, never guessed at.
        public List<string>? Tags { get; set; }
    }

    internal sealed class Registry(params WebMcpToolDescriptor[] tools) : IWebMcpToolRegistry
    {
        public IReadOnlyList<WebMcpToolDescriptor> Tools { get; } = tools;
    }

    internal static WebMcpToolDescriptor[] Tools() =>
    [
        new("list_items", "List items, optionally filtered.", "/items", "GET", true,
            typeof(ListItemsRequest), null),
        new("get_item", "One item by name.", "/items/{Name}", "GET", true,
            typeof(ItemNameRequest), null, ParentListRoute: "/items", ParentListToolName: "list_items"),
        new("create_item", "Create an item of this tool's service type.", "/items", "POST", false,
            typeof(CreateItemRequest), null, PresetFieldName: "ServiceType", PresetValue: "MsSql"),
        new("delete_item", "Delete one item by name.", "/items/{Name}", "DELETE", false,
            typeof(ItemNameRequest), null, ParentListRoute: "/items", ParentListToolName: "list_items"),
        new("ping", "Liveness.", "/ping", "GET", true, null, null),
    ];
}
