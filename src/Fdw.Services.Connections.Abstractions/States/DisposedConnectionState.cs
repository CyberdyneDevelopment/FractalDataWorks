using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection has been disposed and cannot be reused.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionStates), "Disposed", RestrictToCurrentCompilation = true)]
public sealed class DisposedConnectionState() : ConnectionStateBase(8, "Disposed");