using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>Bytes, written as base64.</summary>
[TypeOption(typeof(RecordValueKinds), "Bytes")]
[ExcludeFromCodeCoverage]
public sealed class BytesRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="BytesRecordValueKind"/> class.</summary>
    public BytesRecordValueKind()
        : base("Bytes")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is byte[];

    /// <inheritdoc />
    public override string Text(object value) => Convert.ToBase64String((byte[])value);
}
