using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Represents a calculation input kind — describes the source of data fed into a calculation.
/// </summary>
public interface ICalculationInputKind : ITypeOption<int, CalculationInputKindBase>
{
}
