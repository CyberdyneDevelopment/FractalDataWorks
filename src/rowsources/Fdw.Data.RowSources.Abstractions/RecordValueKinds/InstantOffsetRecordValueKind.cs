using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>A date and time with its offset, in the round-trip format.</summary>
[TypeOption(typeof(RecordValueKinds), "InstantOffset")]
[ExcludeFromCodeCoverage]
public sealed class InstantOffsetRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="InstantOffsetRecordValueKind"/> class.</summary>
    public InstantOffsetRecordValueKind()
        : base("InstantOffset")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override string Text(object value) => ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture);
}
