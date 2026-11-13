using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The script reader of one kind of data store (FDW-5457): a real parse of a script in the store's language, answering the objects it reads,
/// writes and creates, in the terms of the store's catalogue (a path and a container).
/// </summary>
public interface IScriptReader : ITypeOption<int, IScriptReader>
{
    /// <summary>Parses a script and lists the objects it names.</summary>
    /// <param name="script">The script text, with every name given at run time already replaced by one of <paramref name="runTimeNames"/>.</param>
    /// <param name="runTimeNames">The identifiers that stand for a name given at run time; an object named with one is listed in <see cref="ScriptReading.RunTimeNamed"/> only.</param>
    /// <returns>What the script names, or the errors that stopped it parsing.</returns>
    ScriptReading Read(string script, IReadOnlyCollection<string> runTimeNames);
}
