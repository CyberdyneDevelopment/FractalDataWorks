using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.ColumnValues;

/// <summary>A 64-bit whole number.</summary>
[TypeOption(typeof(RowCountKinds), "Int64")]
[ExcludeFromCodeCoverage]
public sealed class Int64RowCountKind : RowCountKindBase
{
    /// <summary>Initializes a new instance of the <see cref="Int64RowCountKind"/> class.</summary>
    public Int64RowCountKind()
        : base("Int64")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is long;

    /// <inheritdoc />
    public override long? Read(object value) => (long)value;
}
