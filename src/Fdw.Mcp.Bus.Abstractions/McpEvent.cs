using System;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>
/// A single event flowing through the MCP bus. Events are immutable, totally ordered per bus
/// instance via <see cref="EventId"/>, and addressed by a hierarchical <see cref="Topic"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Payload"/> is opaque UTF-8 bytes. Sinks discriminate on <see cref="PayloadType"/>
/// to decide how (and whether) to decode. The bus itself never inspects the payload.
/// </para>
/// <para>
/// <see cref="Causation"/> links an event to the event that produced it — saccade chains, tool
/// invocations triggering follow-up events, etc. Used by causation-based replay.
/// </para>
/// </remarks>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record McpEvent(
    ulong          EventId,
    string         Topic,
    DateTimeOffset Timestamp,
    Guid           CorrelationId,
    ulong?         Causation,
    IViewIntent    View,
    string         PayloadType,
    ReadOnlyMemory<byte> Payload);
