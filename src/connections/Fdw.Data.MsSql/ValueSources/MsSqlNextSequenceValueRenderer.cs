using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>
/// Renders the next value of a sequence in the ordered batch form:
/// <c>NEXT VALUE FOR [seq] OVER (ORDER BY alias.[col], ...)</c>. Valid in INSERT ... SELECT.
/// </summary>
[TypeOption(typeof(MsSqlValueSourceRenderers), "NextSequenceValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlNextSequenceValueRenderer : MsSqlValueSourceRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlNextSequenceValueRenderer"/> class.
    /// </summary>
    public MsSqlNextSequenceValueRenderer()
        : base(NextSequenceValueSource.KindName)
    {
    }

    /// <inheritdoc />
    public override string? Render(ColumnValueSource source, MsSqlValueSourceContext context)
    {
        if (source is not NextSequenceValueSource sequence || sequence.OrderBy.Count == 0)
            return null;
        if (!MsSqlIdentifiers.IsSafe(sequence.SequenceName)
            || (sequence.Schema is not null && !MsSqlIdentifiers.IsSafe(sequence.Schema))
            || (sequence.Database is not null && !MsSqlIdentifiers.IsSafe(sequence.Database))
            || MsSqlIdentifiers.FirstUnsafe(sequence.OrderBy) is not null)
            return null;

        var name = MsSqlIdentifiers.Qualified(context.Dialect, sequence.Database, sequence.Schema, sequence.SequenceName);
        var order = string.Join(", ", sequence.OrderBy.Select(c => $"{context.Dialect.QuoteIdentifier(context.SourceAlias)}.{context.Dialect.QuoteIdentifier(c)}"));
        return $"NEXT VALUE FOR {name} OVER (ORDER BY {order})";
    }
}
