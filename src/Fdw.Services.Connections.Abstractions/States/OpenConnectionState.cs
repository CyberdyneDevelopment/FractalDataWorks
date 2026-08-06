using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection is open and ready for use.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionStates), "Open", RestrictToCurrentCompilation = true)]
public sealed class OpenConnectionState() : ConnectionStateBase(3, "Open");