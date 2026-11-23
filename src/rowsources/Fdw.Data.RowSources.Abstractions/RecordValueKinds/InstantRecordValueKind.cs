using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>A date and time; "O" is the round-trip format, which keeps sub-second precision and the Kind, both of which a general-purpose format silently discards.</summary>
[TypeOption(typeof(RecordValueKinds), "Instant")]
[ExcludeFromCodeCoverage]
public sealed class InstantRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="InstantRecordValueKind"/> class.</summary>
    public InstantRecordValueKind()
        : base("Instant")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTime;

    /// <inheritdoc />
    public override string Text(object value) => ((DateTime)value).ToString("O", CultureInfo.InvariantCulture);
}
