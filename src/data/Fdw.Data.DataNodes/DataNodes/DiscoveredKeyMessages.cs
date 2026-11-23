using System;
using Fdw.Messages;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// The coded messages a connection kind gives for what <see cref="DiscoveredContainers"/> finds wrong with the keys of a discovery,
/// so each kind says it in its own codes and the assembly names no connection type.
/// </summary>
/// <param name="TargetNotDiscovered">
/// A warning: a foreign key references a container the discovery did not read. Given the key's name, the schema and name of the
/// container that holds it, and the schema and name of the container it references.
/// </param>
/// <param name="ColumnNotFound">
/// A refusal: a key names a column its container does not have. Given the key's name, the schema and name of the container, and the column.
/// </param>
public sealed record DiscoveredKeyMessages(
    Func<string, string, string, string, string, IGenericMessage> TargetNotDiscovered,
    Func<string, string, string, string, IGenericMessage> ColumnNotFound);
