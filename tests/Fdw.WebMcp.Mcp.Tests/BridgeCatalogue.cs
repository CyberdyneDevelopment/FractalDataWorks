using System.Collections.Generic;
using Fdw.WebMcp.Hosting;

namespace Fdw.WebMcp.Mcp.Tests;

/// <summary>
/// The same shapes the WebMCP golden catalogue uses: a filterable list, a path parameter with a
/// parent list to recover from, a body-carrying tool with a preset, and a bodyless delete.
/// </summary>
internal static class BridgeCatalogue
{
    internal sealed class ListItemsRequest
    {
        public string? ReferenceId { get; set; }

        public int? Take { get; set; }

        public bool? IncludeRetired { get; set; }
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
    }

    internal sealed class Registry(params WebMcpToolDescriptor[] tools) : IWebMcpToolRegistry
    {
        public IReadOnlyList<WebMcpToolDescriptor> Tools { get; } = tools;
    }

    internal static WebMcpToolDescriptor ListItems { get; } =
        new("list_items", "List items.", "/items", "GET", true, typeof(ListItemsRequest), null);

    internal static WebMcpToolDescriptor GetItem { get; } =
        new("get_item", "One item.", "/items/{Name}", "GET", true, typeof(ItemNameRequest), null,
            ParentListRoute: "/items", ParentListToolName: "list_items");

    internal static WebMcpToolDescriptor CreateItem { get; } =
        new("create_item", "Create an item.", "/items", "POST", false, typeof(CreateItemRequest), null,
            PresetFieldName: "ServiceType", PresetValue: "MsSql");

    internal static WebMcpToolDescriptor DeleteItem { get; } =
        new("delete_item", "Delete an item.", "/items/{Name}", "DELETE", false, typeof(ItemNameRequest), null);
}
