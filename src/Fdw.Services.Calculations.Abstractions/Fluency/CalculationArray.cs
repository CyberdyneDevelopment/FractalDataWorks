namespace Fdw.Services.Calculations.Abstractions.Fluency;

/// <summary>
/// The entry point of the calculation array builder: <c>CalculationArray.Named("Settlement").Result("z1", ...).Build(limits)</c>.
/// </summary>
public static class CalculationArray
{
    /// <summary>Starts an array.</summary>
    /// <param name="name">The array name, for logs and refusals.</param>
    /// <returns>The builder.</returns>
    public static CalculationArrayBuilder Named(string name) => new(name);
}
