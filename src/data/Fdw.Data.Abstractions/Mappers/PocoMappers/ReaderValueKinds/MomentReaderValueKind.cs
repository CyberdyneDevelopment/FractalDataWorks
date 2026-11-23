using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>A date and time.</summary>
[TypeOption(typeof(ReaderValueKinds), "Moment")]
[ExcludeFromCodeCoverage]
public sealed class MomentReaderValueKind : ReaderValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="MomentReaderValueKind"/> class.</summary>
    public MomentReaderValueKind()
        : base("Moment")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTime;

    /// <inheritdoc />
    public override DateTime ToDateTime(object value) => (DateTime)value;

    /// <inheritdoc />
    public override DateTimeOffset ToDateTimeOffset(object value, string column) => new((DateTime)value);
}
