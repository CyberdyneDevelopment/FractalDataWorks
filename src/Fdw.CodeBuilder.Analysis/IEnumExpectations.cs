namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for enum expectations.
/// </summary>
public interface IEnumExpectations
{
    /// <summary>
    /// Expects the enum to have a value.
    /// </summary>
    /// <param name="valueName">The value name.</param>
    /// <param name="value">The optional numeric value.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IEnumExpectations HasValue(string valueName, int? value = null);

    /// <summary>
    /// Expects the enum to have a specific underlying type.
    /// </summary>
    /// <param name="underlyingType">The underlying type.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IEnumExpectations HasUnderlyingType(string underlyingType);
}
