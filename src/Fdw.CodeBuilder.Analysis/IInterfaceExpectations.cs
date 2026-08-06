using System;

namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for interface expectations.
/// </summary>
public interface IInterfaceExpectations
{
    /// <summary>
    /// Expects the interface to have a method.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="expectations">Additional expectations for the method.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IInterfaceExpectations HasMethod(string methodName, Action<IMethodExpectations>? expectations = null);

    /// <summary>
    /// Expects the interface to have a property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="expectations">Additional expectations for the property.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IInterfaceExpectations HasProperty(string propertyName, Action<IPropertyExpectations>? expectations = null);
}
