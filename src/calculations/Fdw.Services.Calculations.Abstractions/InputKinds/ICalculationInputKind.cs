using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Represents a calculation input kind — describes the source of data fed into a calculation.
/// </summary>
public interface ICalculationInputKind : ITypeOption<int, CalculationInputKindBase>
{
    /// <summary>
    /// Gets the members of an input this kind takes, in the order a form shows them, so a client draws an input of this kind from them
    /// and create and update refuse an input member that is not listed.
    /// </summary>
    IReadOnlyList<CalculationSetting> Settings { get; }
}
