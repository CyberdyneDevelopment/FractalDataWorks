using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>A bigint, held as a <see cref="Int64"/>.</summary>
[TypeOption(typeof(SqlWholeNumbers), "BigInt")]
[ExcludeFromCodeCoverage]
public sealed class BigIntSqlWholeNumber : SqlWholeNumberBase
{
    /// <summary>Initializes a new instance of the <see cref="BigIntSqlWholeNumber"/> class.</summary>
    public BigIntSqlWholeNumber()
        : base("BigInt", typeof(long))
    {
    }

    /// <inheritdoc/>
    public override object BitwiseNot(object operand) => ~(long)operand;
}
