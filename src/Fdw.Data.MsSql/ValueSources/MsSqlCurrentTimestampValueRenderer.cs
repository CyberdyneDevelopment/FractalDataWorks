using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>Renders the current timestamp as SYSDATETIMEOFFSET().</summary>
[TypeOption(typeof(MsSqlValueSourceRenderers), "CurrentTimestamp", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlCurrentTimestampValueRenderer : MsSqlValueSourceRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlCurrentTimestampValueRenderer"/> class.
    /// </summary>
    public MsSqlCurrentTimestampValueRenderer()
        : base(CurrentTimestampValueSource.KindName)
    {
    }

    /// <inheritdoc />
    public override string? Render(ColumnValueSource source, MsSqlValueSourceContext context) =>
        source is CurrentTimestampValueSource ? "SYSDATETIMEOFFSET()" : null;
}
