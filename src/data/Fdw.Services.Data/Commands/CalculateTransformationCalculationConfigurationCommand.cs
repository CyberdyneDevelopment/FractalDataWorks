using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes <c>transform.CalculateTransformationConfigurationCalculation</c> rows as <see cref="CalculateTransformationCalculationConfiguration"/>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "CalculateTransformationConfigurationCalculation")]
public sealed class CalculateTransformationCalculationConfigurationCommand : ConfigurationCommandBase<CalculateTransformationCalculationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CalculateTransformationCalculationConfigurationCommand"/> class.</summary>
    public CalculateTransformationCalculationConfigurationCommand() : base("CalculateTransformationConfigurationCalculation") { }
}
