using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>A tinyint, held as a <see cref="Byte"/>.</summary>
[TypeOption(typeof(SqlWholeNumbers), "TinyInt")]
[ExcludeFromCodeCoverage]
public sealed class TinyIntSqlWholeNumber : SqlWholeNumberBase
{
    /// <summary>Initializes a new instance of the <see cref="TinyIntSqlWholeNumber"/> class.</summary>
    public TinyIntSqlWholeNumber()
        : base("TinyInt", typeof(byte))
    {
    }

    /// <inheritdoc/>
    public override object BitwiseNot(object operand) => ~(byte)operand;
}
