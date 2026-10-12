using System.Collections.Generic;
using Fdw.Configuration;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>A calculation step as the executor reads it, whichever implementation owns it.</summary>
/// <remarks>
/// Each implementation's step type holds its own field and operand types, so it presents them here
/// through their shared bases.
/// </remarks>
public interface ICalculationStepConfiguration : IGenericConfiguration
{
    /// <summary>Gets the name of this step.</summary>
    string Name { get; }

    /// <summary>Gets the operation type name from <see cref="CalculationOperationTypes"/>.</summary>
    string OperationType { get; }

    /// <summary>Gets the ordinal position of this step; steps execute in ascending order.</summary>
    int Ordinal { get; }

    /// <summary>Gets the alias this step's output is published under.</summary>
    string OutputAlias { get; }

    /// <summary>Gets the fields this step groups or orders by.</summary>
    IReadOnlyList<CalculationStepFieldConfigurationBase> Fields { get; }

    /// <summary>Gets the operands bound to this step.</summary>
    IReadOnlyList<CalculationStepOperandConfigurationBase> Operands { get; }
}
