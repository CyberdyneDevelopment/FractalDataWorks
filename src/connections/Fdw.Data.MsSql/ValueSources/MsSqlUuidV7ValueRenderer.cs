using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>
/// Renders a new UUIDv7 for each row as one T-SQL expression: the UTC time in milliseconds since 1970 (48 bits) followed by random bits
/// from <c>NEWID()</c>, with the version nibble set to 7 and the variant to 10. The expression is built as the canonical text and cast, so
/// SQL Server's own byte order for <c>uniqueidentifier</c> shows the same text a .NET <c>Guid.CreateVersion7()</c> does. The time is read once
/// for the statement, so the rows of one batch share a time prefix and differ in their random bits.
/// </summary>
[TypeOption(typeof(MsSqlValueSourceRenderers), "UuidV7", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlUuidV7ValueRenderer : MsSqlValueSourceRendererBase
{
    // 12 hex characters of time + 20 of random make 32 hex characters; the version goes at 13, the variant at 17, then the four dashes
    // are put in from the left, each at its place in the growing text.
    internal const string Expression =
        "CAST(STUFF(STUFF(STUFF(STUFF(STUFF(STUFF("
        + "LEFT(CONVERT(char(12), CAST(DATEDIFF_BIG(MILLISECOND, '19700101', SYSUTCDATETIME()) AS binary(6)), 2)"
        + " + CONVERT(char(32), CAST(NEWID() AS binary(16)), 2), 32)"
        + ", 13, 1, '7'), 17, 1, SUBSTRING('89AB', 1 + (CHECKSUM(NEWID()) & 3), 1))"
        + ", 9, 0, '-'), 14, 0, '-'), 19, 0, '-'), 24, 0, '-') AS uniqueidentifier)";

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlUuidV7ValueRenderer"/> class.
    /// </summary>
    public MsSqlUuidV7ValueRenderer()
        : base(UuidV7ValueSource.KindName)
    {
    }

    /// <inheritdoc />
    public override string? Render(ColumnValueSource source, MsSqlValueSourceContext context) =>
        source is UuidV7ValueSource ? Expression : null;
}
