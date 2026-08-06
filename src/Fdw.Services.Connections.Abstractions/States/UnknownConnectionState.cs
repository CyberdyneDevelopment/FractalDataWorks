using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection is in an unknown or uninitialized state.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionStates), "Unknown", RestrictToCurrentCompilation = true)]
public sealed class UnknownConnectionState() : ConnectionStateBase(0, "Unknown");