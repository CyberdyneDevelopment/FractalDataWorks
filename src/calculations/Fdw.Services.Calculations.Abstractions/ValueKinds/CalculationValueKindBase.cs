using System;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Base class for the kinds of value a calculation result can hold (CRTP pattern).
/// </summary>
/// <remarks>
/// Why the results array stays a decimal array: it is the hot path of a run over many records, and every consumer written before
/// date-times existed reads it as numbers. A kind that is not a number says how its value is carried in a decimal and reads it
/// back, so a consumer asks the kind and never switches on a type.
/// </remarks>
public abstract class CalculationValueKindBase : TypeOptionBase<CalculationValueKindBase>, ICalculationValueKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationValueKindBase"/> class.
    /// </summary>
    /// <param name="name">The kind's name.</param>
    /// <param name="clrType">The type the compiled code computes the value as.</param>
    /// <param name="dataTypeName">The data type name a field of this kind states.</param>
    protected CalculationValueKindBase(string name, Type clrType, string dataTypeName)
        : base(name)
    {
        ClrType = clrType;
        DataTypeName = dataTypeName;
    }

    /// <inheritdoc />
    public Type ClrType { get; }

    /// <inheritdoc />
    public string DataTypeName { get; }

    /// <inheritdoc />
    public abstract object Read(decimal carried);
}
