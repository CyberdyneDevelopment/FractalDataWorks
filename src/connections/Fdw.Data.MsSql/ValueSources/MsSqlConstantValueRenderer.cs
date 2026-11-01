using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>Renders a constant as a parameter (or a NULL literal).</summary>
[TypeOption(typeof(MsSqlValueSourceRenderers), "Constant", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlConstantValueRenderer : MsSqlValueSourceRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConstantValueRenderer"/> class.
    /// </summary>
    public MsSqlConstantValueRenderer()
        : base(ConstantValueSource.KindName)
    {
    }

    /// <inheritdoc />
    public override string? Render(ColumnValueSource source, MsSqlValueSourceContext context)
    {
        if (source is not ConstantValueSource constant)
            return null;
        if (constant.Value is null)
            return "NULL";

        var name = context.NextParameterName("const");
        context.Command.Parameters.Add(new SqlParameter(name, constant.Value));
        return name;
    }
}
