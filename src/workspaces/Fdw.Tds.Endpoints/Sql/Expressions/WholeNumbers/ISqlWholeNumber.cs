using System;
using Fdw.Collections;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>One .NET type a T-SQL whole number is held in, and what ~ makes of a value of it.</summary>
public interface ISqlWholeNumber : ITypeOption<int, SqlWholeNumberBase>
{
    /// <summary>Gets the .NET type the number is held in.</summary>
    Type ClrType { get; }

    /// <summary>Applies ~ to a value.</summary>
    /// <param name="operand">A value of <see cref="ClrType"/>.</param>
    /// <returns>The complement: a bigint for a bigint, an int for any smaller whole number.</returns>
    object BitwiseNot(object operand);
}
