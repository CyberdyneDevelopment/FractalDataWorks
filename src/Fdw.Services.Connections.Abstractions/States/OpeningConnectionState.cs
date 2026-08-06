using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection is currently being opened.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionStates), "Opening", RestrictToCurrentCompilation = true)]
public sealed class OpeningConnectionState() : ConnectionStateBase(2, "Opening");