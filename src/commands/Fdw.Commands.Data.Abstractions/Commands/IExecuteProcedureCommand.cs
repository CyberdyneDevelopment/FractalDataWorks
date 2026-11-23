using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A command that runs a named stored procedure of a store, given scalar parameters (FDW-6682).
/// </summary>
public interface IExecuteProcedureCommand : IDataCommand
{
    /// <summary>Gets the parameters by name, without the leading <c>@</c>. A value is a scalar (text, boolean, number, Guid, date) or null; null is stated, never left out.</summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }
}
