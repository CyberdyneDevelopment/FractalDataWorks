namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for method expectations.
/// </summary>
public interface IMethodExpectations
{
    /// <summary>
    /// Expects the method to have a specific return type.
    /// </summary>
    /// <param name="returnType">The expected return type.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IMethodExpectations HasReturnType(string returnType);

    /// <summary>
    /// Expects the method to have a parameter.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="parameterType">The parameter type.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IMethodExpectations HasParameter(string parameterName, string parameterType);

    /// <summary>
    /// Expects the method to be async.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
#pragma warning disable FDW001 // "Async" suffix is descriptive here - indicates async expectation, not an async method
    IMethodExpectations IsAsync();
#pragma warning restore FDW001

    /// <summary>
    /// Expects the method to be static.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IMethodExpectations IsStatic();

    /// <summary>
    /// Expects the method to be virtual.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IMethodExpectations IsVirtual();

    /// <summary>
    /// Expects the method to be override.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IMethodExpectations IsOverride();

    /// <summary>
    /// Expects the method to be abstract.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IMethodExpectations IsAbstract();
}
