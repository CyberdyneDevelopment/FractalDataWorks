using System;

namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for record expectations.
/// </summary>
public interface IRecordExpectations
{
    /// <summary>
    /// Expects the record to have a property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="expectations">Additional expectations for the property.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IRecordExpectations HasProperty(string propertyName, Action<IPropertyExpectations>? expectations = null);

    /// <summary>
    /// Expects the record to have a constructor parameter.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="parameterType">The parameter type.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IRecordExpectations HasParameter(string parameterName, string parameterType);
}
