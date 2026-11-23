using System.Collections.Generic;
using System.Linq;
using Fdw.WebMcp.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.WebMcp.Mcp.Tests;

/// <summary>
/// The same shapes the WebMCP golden catalogue uses: a filterable list, a path parameter with a
/// parent list to recover from, a body-carrying tool with a preset, and a bodyless delete.
/// </summary>
internal static class BridgeCatalog
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

    /// <summary>The nesting the tests allow.</summary>
    internal const int MaxDepth = 3;

    internal sealed class Registry(params WebMcpToolDescriptor[] tools) : IWebMcpToolRegistry
    {
        public IReadOnlyList<WebMcpToolDescriptor> Tools { get; } = tools;

        public IReadOnlyList<WebMcpToolPlan> Plans { get; } =
            [.. tools.Select(tool => WebMcpToolPlan.Create(tool, MaxDepth, NullLogger.Instance).Value.ShouldNotBeNull())];
    }

    internal sealed class Attendee
    {
        public required string Email { get; set; }

        public int Seats { get; set; }
    }

    internal sealed class MeetingRequest
    {
        public string? Title { get; set; }

        public List<string>? Recipients { get; set; }

        public required List<Attendee> Attendees { get; set; }
    }

    internal static WebMcpToolDescriptor CreateMeeting { get; } =
        new("create_meeting", "Create a meeting.", "/meetings", "POST", false, typeof(MeetingRequest), null);

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
