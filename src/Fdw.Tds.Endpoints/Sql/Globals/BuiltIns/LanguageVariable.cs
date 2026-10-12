using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@LANGUAGE: the session language.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "Language", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LanguageVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="LanguageVariable"/> class.</summary>
    public LanguageVariable()
        : base(name: "Language", sqlName: "@@LANGUAGE")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => context.Server.Language;
}
