using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A script that is a query (FDW-1435): it answers the rows the text selects, each row its columns by name.
/// </summary>
public interface IScriptReadCommand : IScriptedDataCommand<IReadOnlyList<IDictionary<string, object?>>>
{
    /// <summary>
    /// Gets the most rows to read; the read fails when the script selects more, so a script that was meant to select a few
    /// cannot load a table. Null states no limit.
    /// </summary>
    int? Take { get; }
}
