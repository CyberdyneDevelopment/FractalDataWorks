using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// An exact decimal number: the kind every calculation result had before date-times.
/// </summary>
[TypeOption(typeof(CalculationValueKinds), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberCalculationValueKind : CalculationValueKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NumberCalculationValueKind"/> class.
    /// </summary>
    public NumberCalculationValueKind()
        : base("Number", typeof(decimal), "Decimal")
    {
    }

    /// <inheritdoc />
    public override object Read(decimal carried) => carried;
}
