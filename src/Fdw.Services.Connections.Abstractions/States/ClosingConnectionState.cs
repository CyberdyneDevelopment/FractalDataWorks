using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection is currently being closed.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionStates), "Closing", RestrictToCurrentCompilation = true)]
public sealed class ClosingConnectionState() : ConnectionStateBase(5, "Closing");