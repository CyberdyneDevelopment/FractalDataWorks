using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>A length of time, in the constant format.</summary>
[TypeOption(typeof(RecordValueKinds), "Duration")]
[ExcludeFromCodeCoverage]
public sealed class DurationRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DurationRecordValueKind"/> class.</summary>
    public DurationRecordValueKind()
        : base("Duration")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is TimeSpan;

    /// <inheritdoc />
    public override string Text(object value) => ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture);
}
