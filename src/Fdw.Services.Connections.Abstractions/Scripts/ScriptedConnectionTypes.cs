using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The script extensions of the database connections (FDW-1447), one for each kind of connection that can take a script.
/// </summary>
/// <remarks>
/// A script is not general: it is for ETL, and only the host that references an extension package has its options. A host
/// that references none has an empty collection, so the gateway there answers that its stores take no scripts. The shared
/// connections do not implement <see cref="IScriptedDataConnection"/>; the extensions do, around them.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ScriptedConnectionTypeBase), typeof(IScriptedConnectionType), typeof(ScriptedConnectionTypes), RestrictToCurrentCompilation = false)]
public abstract partial class ScriptedConnectionTypes : TypeCollectionBase<ScriptedConnectionTypeBase, IScriptedConnectionType>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
