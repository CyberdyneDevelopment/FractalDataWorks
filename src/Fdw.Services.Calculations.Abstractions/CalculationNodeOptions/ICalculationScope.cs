using System;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// What a node's check may ask of the array it sits in: which results are defined before it, and a place to note the input
/// fields it reads.
/// </summary>
public interface ICalculationScope
{
    /// <summary>Gets a value indicating whether a result of that name is defined before the node being checked.</summary>
    /// <param name="result">The result name.</param>
    /// <returns>True when a Ref to it is allowed.</returns>
    bool IsDefined(string result);

    /// <summary>Gets the type a result defined before the node being checked yields.</summary>
    /// <param name="result">The result name.</param>
    /// <returns>The type, decimal for a number or DateTimeOffset for a date-time; null when no such result is defined.</returns>
    Type? TypeOf(string result);

    /// <summary>Notes that the array reads an input field.</summary>
    /// <param name="field">The field name.</param>
    void Reads(string field);
}
