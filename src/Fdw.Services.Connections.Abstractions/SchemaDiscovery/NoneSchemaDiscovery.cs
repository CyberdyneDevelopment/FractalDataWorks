using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection does not discover the schema of the store it opens: its containers and fields are
/// declared by hand.
/// </summary>
/// <remarks>
/// <para>
/// The base position of every connection type (<c>ConnectionTypeBase.SupportedDiscoveryTypes</c>),
/// and the opt-out a connection type that does discover also declares, so one of its connections can
/// decline.
/// </para>
/// <para>
/// A real member rather than an empty list or a null, for the reason <see cref="NoneSessionContext"/>
/// gives: "this connection does not discover" is a position that is stated, never an absence a reader
/// has to infer. An empty list would look the same as a connection type that forgot to declare its
/// options.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaDiscoveryTypes), "None")]
public sealed class NoneSchemaDiscovery() : SchemaDiscoveryTypeBase(
    name: "None",
    displayName: "No schema discovery",
    description: "Containers and fields are declared by hand; nothing is read from the store to find them.",
    discoversSchema: false);
