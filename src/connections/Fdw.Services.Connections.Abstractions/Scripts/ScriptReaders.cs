using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The script readers (FDW-5457), one for each kind of data store whose scripts can be parsed: each reads a script in the language of its store
/// and says what the script reads and writes.
/// </summary>
/// <remarks>
/// An option is named as the kind of data store it reads for (the name its data store type has), so code above the connection layer finds the
/// reader by the store's kind and never names a connection type. A host that references no package with a reader for a kind has none for it.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ScriptReaderBase), typeof(IScriptReader), typeof(ScriptReaders), RestrictToCurrentCompilation = false)]
public abstract partial class ScriptReaders : TypeCollectionBase<ScriptReaderBase, IScriptReader>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
