using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A data command that can ask for columns of the rows it wrote: the generated key a database filled in, a column default,
/// the value an update left. Without any it answers the number of rows written, as every command does.
/// </summary>
/// <remarks>
/// With columns named, the command answers <c>IReadOnlyList&lt;IReadOnlyDictionary&lt;string, object?&gt;&gt;</c>: one
/// dictionary for each row written, holding exactly the columns named. A column the container does not declare is refused
/// with a named result, never skipped.
/// </remarks>
public interface IReturningCommand : IDataCommand
{
    /// <summary>Gets the names of the columns to return for each row written; none or empty answers a row count.</summary>
    IReadOnlyList<string>? ReturnColumns { get; }
}
