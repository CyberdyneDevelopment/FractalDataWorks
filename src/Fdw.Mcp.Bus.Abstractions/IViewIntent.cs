using Fdw.Collections;

namespace Fdw.Mcp.Bus.Abstractions;

/// <summary>
/// Per-event directive controlling whether view-bound sinks (e.g. the Pidgin canvas) project
/// this event. Stdio and other RPC-response sinks ignore <see cref="IViewIntent"/> — they always
/// deliver. New intents (Pulse, Lock, Pin, etc.) land as additional <see cref="ViewIntents"/>
/// TypeOptions.
/// </summary>
public interface IViewIntent : ITypeOption<int, IViewIntent>
{
}
