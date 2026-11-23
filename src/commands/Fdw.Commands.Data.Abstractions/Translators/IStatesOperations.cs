using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A command translator that states the operations the command it translates performs (FDW-6682).
/// </summary>
/// <remarks>
/// Why: a container's type declares the operations it supports (<c>IContainerType.SupportedOperations</c>), and nothing judged a command against them. The connection asks
/// <c>DataOperationRule</c> with this list before it calls <c>Translate</c>, so a command whose container does not state every operation it performs is refused with a
/// coded message and nothing is built or sent. The list is read from what the translator emits: an upsert is an insert and an update, a find is a query. A translator
/// that states none is not asked, so a connection kind adopts the rule by implementing this interface on its translators.
/// </remarks>
public interface IStatesOperations
{
    /// <summary>Gets the operations (<c>Query</c>, <c>Insert</c>, <c>Update</c>, <c>Delete</c>, <c>Execute</c>) the command performs, as a container type names them.</summary>
    IReadOnlyList<string> Operations { get; }
}
