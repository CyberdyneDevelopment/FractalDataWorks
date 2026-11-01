using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>A script that is a statement that changes data and answers how many rows it affected.</summary>
[TypeOption(typeof(ScriptKinds), "NonQuery")]
public sealed class NonQueryScriptKind : ScriptKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NonQueryScriptKind"/> class.</summary>
    public NonQueryScriptKind() : base("NonQuery", "Changes data and answers how many rows it affected.", answersRows: false)
    {
    }
}
