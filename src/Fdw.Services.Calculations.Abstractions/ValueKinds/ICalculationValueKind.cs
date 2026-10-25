using System;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// A kind of value a calculation result can hold: the type the compiled code computes it as, the data type name a consumer
/// checks it against, and how a result carried in the results array turns back into the value.
/// </summary>
public interface ICalculationValueKind : ITypeOption<int, CalculationValueKindBase>
{
    /// <summary>Gets the type the compiled code computes the value as.</summary>
    Type ClrType { get; }

    /// <summary>Gets the data type name a data set field of this kind states, for example <c>Decimal</c> or <c>DateTimeOffset</c>.</summary>
    string DataTypeName { get; }

    /// <summary>Turns a result carried in the results array back into the value.</summary>
    /// <param name="carried">The carried result.</param>
    /// <returns>The value, boxed.</returns>
    object Read(decimal carried);
}
