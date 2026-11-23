using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>
/// The document commands a narrowed store can keep inside its folder, one handler for each command type, found by the command's
/// <c>CommandType</c>. A command type with no handler is refused, never passed through.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(NarrowingCommandHandlerBase), typeof(INarrowingCommandHandler), typeof(NarrowingCommandHandlers))]
public abstract partial class NarrowingCommandHandlers : TypeCollectionBase<NarrowingCommandHandlerBase, INarrowingCommandHandler>
{
}
