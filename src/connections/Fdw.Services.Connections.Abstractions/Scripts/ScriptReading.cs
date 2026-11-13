using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// What a script names, as its reader parsed it (FDW-5457). Each list holds a name once; a script that did not parse has its errors and no names.
/// </summary>
/// <param name="Errors">Why the script did not parse; empty when it parsed.</param>
/// <param name="Reads">The objects it reads rows from.</param>
/// <param name="Writes">The objects it changes rows of (an insert, update, delete, merge, truncate, or the target of a select into).</param>
/// <param name="Creates">The objects it creates itself, which need not exist before it runs.</param>
/// <param name="OutsideStore">The names it gives of objects in another database or server, which the store's catalogue cannot hold.</param>
/// <param name="RunTimeNamed">The names it gives with a part given at run time, which no catalogue can judge before the run.</param>
public sealed record ScriptReading(
    IReadOnlyList<ScriptParseError> Errors,
    IReadOnlyList<ScriptObjectName> Reads,
    IReadOnlyList<ScriptObjectName> Writes,
    IReadOnlyList<ScriptObjectName> Creates,
    IReadOnlyList<string> OutsideStore,
    IReadOnlyList<string> RunTimeNamed)
{
    /// <summary>Gets a value indicating whether the script parsed.</summary>
    public bool Parsed => Errors.Count == 0;
}
