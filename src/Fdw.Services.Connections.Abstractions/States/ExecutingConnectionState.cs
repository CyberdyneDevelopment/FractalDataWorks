using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection is currently executing an operation.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionStates), "Executing", RestrictToCurrentCompilation = true)]
public sealed class ExecutingConnectionState() : ConnectionStateBase(4, "Executing");