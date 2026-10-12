using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>The contract every CalculationEntity implementation carries.</summary>
public interface ICalculationEntityImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets where the calculation came from.</summary>
    string CalculationSource { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the data set the result is written to.</summary>
    string? OutputDataSetName { get; set; }

    /// <summary>Gets or sets the field the result is written to.</summary>
    string? ResultFieldName { get; set; }

    /// <summary>Gets or sets the data type the result is written as.</summary>
    string ResultDataTypeName { get; set; }

    /// <summary>Gets or sets whether the calculation runs.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets the inputs the calculation reads, held in the implementation's own input table.</summary>
    IReadOnlyList<CalculationInputConfigurationBase> Inputs { get; }

    /// <summary>Gets the steps the calculation executes, held in the implementation's own step table.</summary>
    IReadOnlyList<ICalculationStepConfiguration> Steps { get; }

    /// <summary>Replaces the inputs with these, as inputs of this implementation.</summary>
    /// <param name="inputs">The declared inputs, in order.</param>
    void ReplaceInputs(IReadOnlyList<CalculationInput> inputs);
}
