namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for property expectations.
/// </summary>
public interface IPropertyExpectations
{
    /// <summary>
    /// Expects the property to have a specific type.
    /// </summary>
    /// <param name="propertyType">The expected property type.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IPropertyExpectations HasType(string propertyType);

    /// <summary>
    /// Expects the property to have a getter.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IPropertyExpectations HasGetter();

    /// <summary>
    /// Expects the property to have a setter.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IPropertyExpectations HasSetter();

    /// <summary>
    /// Expects the property to have an init setter.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IPropertyExpectations HasInitSetter();

    /// <summary>
    /// Expects the property to be read-only.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IPropertyExpectations IsReadOnly();

    /// <summary>
    /// Expects the property to be static.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IPropertyExpectations IsStatic();
}
