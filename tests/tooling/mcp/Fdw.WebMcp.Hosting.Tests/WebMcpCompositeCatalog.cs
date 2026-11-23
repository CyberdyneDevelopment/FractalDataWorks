using System.Collections.Generic;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// Request types whose members are lists and objects: a meeting with recipients, attendees and a recurrence, a type that
/// refers to itself, and a chain deeper than a tool may be.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="WebMcpGoldenCatalog"/>, whose script is pinned byte for byte: a tool with only scalars must
/// describe itself exactly as it did before lists and objects were described.
/// </remarks>
internal static class WebMcpCompositeCatalog
{
    /// <summary>The nesting the tests allow: more than the meeting needs (2), less than the chain <see cref="Deep"/> has (4).</summary>
    internal const int MaxDepth = 3;

    internal sealed class Attendee
    {
        public required string Email { get; set; }

        public int Seats { get; set; }

        public string? Note { get; set; }
    }

    internal sealed class Recurrence
    {
        public string? Pattern { get; set; }

        public int Count { get; set; }
    }

    internal sealed class MeetingRequest
    {
        public string? Title { get; set; }

        public List<string>? Recipients { get; set; }

        public required List<Attendee> Attendees { get; set; }

        public Recurrence? Recurrence { get; set; }

        // No JSON Schema shape describes a dictionary or raw bytes; each must be skipped, never guessed at.
        public Dictionary<string, string>? Labels { get; set; }

        public byte[]? Blob { get; set; }
    }

    internal sealed class MeetingByRoomRequest
    {
        public string? Room { get; set; }

        public List<Attendee>? Attendees { get; set; }
    }

    internal sealed class Node
    {
        public string? Name { get; set; }

        public Node? Next { get; set; }
    }

    internal sealed class Tree
    {
        public string? Name { get; set; }

        public List<Tree>? Children { get; set; }
    }

    internal sealed class LevelThree
    {
        public string? Value { get; set; }
    }

    internal sealed class LevelTwo
    {
        public LevelThree? Inner { get; set; }
    }

    internal sealed class LevelOne
    {
        public List<LevelTwo>? Items { get; set; }
    }

    internal sealed class DeepRequest
    {
        public LevelOne? First { get; set; }
    }

    internal static WebMcpToolDescriptor Meeting { get; } =
        new("create_meeting", "Create a meeting.", "/meetings", "POST", false, typeof(MeetingRequest), null);

    internal static WebMcpToolDescriptor MeetingByRoom { get; } =
        new("book_room", "Book a room.", "/rooms/{Room}/meetings", "POST", false, typeof(MeetingByRoomRequest), null);

    internal static WebMcpToolDescriptor Cyclic { get; } =
        new("walk_nodes", "Walk nodes.", "/nodes", "POST", false, typeof(Node), null);

    internal static WebMcpToolDescriptor CyclicThroughAList { get; } =
        new("walk_tree", "Walk a tree.", "/trees", "POST", false, typeof(Tree), null);

    // Four levels of list or object below the request: First (object), Items (list), each item (object), Inner (object).
    internal static WebMcpToolDescriptor Deep { get; } =
        new("go_deep", "Too deep.", "/deep", "POST", false, typeof(DeepRequest), null);
}
