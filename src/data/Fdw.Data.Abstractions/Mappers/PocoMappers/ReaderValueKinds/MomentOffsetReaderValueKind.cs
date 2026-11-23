using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>A date and time with an offset.</summary>
[TypeOption(typeof(ReaderValueKinds), "MomentOffset")]
[ExcludeFromCodeCoverage]
public sealed class MomentOffsetReaderValueKind : ReaderValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="MomentOffsetReaderValueKind"/> class.</summary>
    public MomentOffsetReaderValueKind()
        : base("MomentOffset")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override DateTime ToDateTime(object value) => ((DateTimeOffset)value).UtcDateTime;

    /// <inheritdoc />
    public override DateTimeOffset ToDateTimeOffset(object value, string column) => (DateTimeOffset)value;
}
