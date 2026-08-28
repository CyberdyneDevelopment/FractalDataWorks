using System;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>
/// A publisher-supplied event payload that the bus will assign an <see cref="McpEvent.EventId"/>
/// and <see cref="McpEvent.Timestamp"/> to on <see cref="IMcpEventBus.Publish"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record McpEventDraft(
    string         Topic,
    Guid           CorrelationId,
    ulong?         Causation,
    IViewIntent    View,
    string         PayloadType,
    ReadOnlyMemory<byte> Payload);
