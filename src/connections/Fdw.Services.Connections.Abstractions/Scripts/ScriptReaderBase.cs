using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Base class for the options of <see cref="ScriptReaders"/>, using the CRTP pattern.
/// </summary>
public abstract class ScriptReaderBase : TypeOptionBase<ScriptReaderBase>, IScriptReader
{
    /// <summary>Initializes a new instance of the <see cref="ScriptReaderBase"/> class.</summary>
    /// <param name="name">The kind of data store this reader is for, named as its data store type is.</param>
    protected ScriptReaderBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public abstract ScriptReading Read(string script, IReadOnlyCollection<string> runTimeNames);
}
