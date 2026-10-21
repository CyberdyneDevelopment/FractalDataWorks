using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>The kinds of stored script (FDW-1442).</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ScriptKindBase), typeof(IScriptKind), typeof(ScriptKinds))]
public abstract partial class ScriptKinds : TypeCollectionBase<ScriptKindBase, IScriptKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
