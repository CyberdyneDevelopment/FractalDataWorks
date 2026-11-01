using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Base class for the options of <see cref="ScriptedConnectionTypes"/>, using the CRTP pattern.
/// </summary>
public abstract class ScriptedConnectionTypeBase : TypeOptionBase<ScriptedConnectionTypeBase>, IScriptedConnectionType
{
    /// <summary>Initializes a new instance of the <see cref="ScriptedConnectionTypeBase"/> class.</summary>
    /// <param name="name">The kind of connection this extension is for, named as its connection type is (for example <c>MsSql</c>).</param>
    protected ScriptedConnectionTypeBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IScriptedDataConnection? Adapt(IDataConnection connection);
}
