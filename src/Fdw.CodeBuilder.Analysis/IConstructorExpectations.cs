namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for constructor expectations.
/// </summary>
public interface IConstructorExpectations
{
    /// <summary>
    /// Expects the constructor to have a parameter.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="parameterType">The parameter type.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IConstructorExpectations HasParameter(string parameterName, string parameterType);

    /// <summary>
    /// Expects the constructor to be static.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IConstructorExpectations IsStatic();
}
