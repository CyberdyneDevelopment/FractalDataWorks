using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@LANGID: the id of the session language; us_english is 0.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "LangId", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LangIdVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="LangIdVariable"/> class.</summary>
    public LangIdVariable()
        : base(name: "LangId", sqlName: "@@LANGID")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => (short)0;
}
