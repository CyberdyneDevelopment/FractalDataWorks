using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection is in a broken or faulted state and cannot be used.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionStates), "Broken", RestrictToCurrentCompilation = true)]
public sealed class BrokenConnectionState() : ConnectionStateBase(7, "Broken");