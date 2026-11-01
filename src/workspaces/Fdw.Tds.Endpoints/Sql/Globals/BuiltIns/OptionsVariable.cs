using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@OPTIONS: the SET options in effect, as SQL Server's bit mask.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "Options", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OptionsVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="OptionsVariable"/> class.</summary>
    public OptionsVariable()
        : base(name: "Options", sqlName: "@@OPTIONS")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => SessionOptions.Mask(context.Session);
}
