using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>Renders a source field as the source alias's column: <c>alias.[field]</c>.</summary>
[TypeOption(typeof(MsSqlValueSourceRenderers), "Column", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlColumnValueRenderer : MsSqlValueSourceRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlColumnValueRenderer"/> class.
    /// </summary>
    public MsSqlColumnValueRenderer()
        : base(ColumnReferenceValueSource.KindName)
    {
    }

    /// <inheritdoc />
    public override string? Render(ColumnValueSource source, MsSqlValueSourceContext context) =>
        source is ColumnReferenceValueSource column && MsSqlIdentifiers.IsSafe(column.SourceField)
            ? $"{context.Dialect.QuoteIdentifier(context.SourceAlias)}.{context.Dialect.QuoteIdentifier(column.SourceField)}"
            : null;
}
