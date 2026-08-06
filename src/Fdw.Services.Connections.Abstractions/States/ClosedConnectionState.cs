using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection is closed.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionStates), "Closed", RestrictToCurrentCompilation = true)]
public sealed class ClosedConnectionState() : ConnectionStateBase(6, "Closed");