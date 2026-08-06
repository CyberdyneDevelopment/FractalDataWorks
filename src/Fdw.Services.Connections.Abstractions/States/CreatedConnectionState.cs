using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection has been created but not yet initialized or opened.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionStates), "Created", RestrictToCurrentCompilation = true)]
public sealed class CreatedConnectionState() : ConnectionStateBase(1, "Created");