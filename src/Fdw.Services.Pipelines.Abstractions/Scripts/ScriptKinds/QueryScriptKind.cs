using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>A script that selects rows.</summary>
[TypeOption(typeof(ScriptKinds), "Query")]
public sealed class QueryScriptKind : ScriptKindBase
{
    /// <summary>Initializes a new instance of the <see cref="QueryScriptKind"/> class.</summary>
    public QueryScriptKind() : base("Query", "Selects rows and answers them.", answersRows: true)
    {
    }
}
