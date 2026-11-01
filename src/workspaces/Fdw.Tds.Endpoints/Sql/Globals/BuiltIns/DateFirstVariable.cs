using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@DATEFIRST: the first day of the week.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "DateFirst", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DateFirstVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="DateFirstVariable"/> class.</summary>
    public DateFirstVariable()
        : base(name: "DateFirst", sqlName: "@@DATEFIRST")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => (byte)context.Session.DateFirst;
}
