using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@TEXTSIZE: the most bytes of text a value returns.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "TextSize", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TextSizeVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="TextSizeVariable"/> class.</summary>
    public TextSizeVariable()
        : base(name: "TextSize", sqlName: "@@TEXTSIZE")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => context.Session.TextSize;
}
