using System;
using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A cross-field text search over a container: the rows that hold the search term, each with the names of the fields
/// that held it (FDW-1361).
/// </summary>
/// <remarks>
/// A connection runs the search its own way and reads each row as <see cref="RowType"/> together with the names of the
/// fields that matched, measured where the search ran. It hands both to <see cref="Results"/>, which builds the
/// command's own result, so the connection needs no knowledge of it.
/// </remarks>
public interface IFindCommand : IDataCommand
{
    /// <summary>Gets the type each row is read as.</summary>
    Type RowType { get; }

    /// <summary>The command's result from the rows read and the fields that matched in each, in the order read.</summary>
    /// <param name="rows">Each row, read as <see cref="RowType"/>.</param>
    /// <param name="matchedFields">The names of the fields that matched in each row, by position.</param>
    /// <returns>The command's result.</returns>
    object Results(IReadOnlyList<object> rows, IReadOnlyList<IReadOnlyList<string>> matchedFields);
}
