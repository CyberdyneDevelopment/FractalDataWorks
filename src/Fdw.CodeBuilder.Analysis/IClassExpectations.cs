using System;

namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for class expectations.
/// </summary>
public interface IClassExpectations
{
    /// <summary>
    /// Expects the class to have a specific access modifier.
    /// </summary>
    /// <param name="modifier">The expected access modifier.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations HasAccessModifier(string modifier);

    /// <summary>
    /// Expects the class to have a method.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="expectations">Additional expectations for the method.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations HasMethod(string methodName, Action<IMethodExpectations>? expectations = null);

    /// <summary>
    /// Expects the class to have a property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="expectations">Additional expectations for the property.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations HasProperty(string propertyName, Action<IPropertyExpectations>? expectations = null);

    /// <summary>
    /// Expects the class to have a field.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="expectations">Additional expectations for the field.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations HasField(string fieldName, Action<IFieldExpectations>? expectations = null);

    /// <summary>
    /// Expects the class to have a constructor.
    /// </summary>
    /// <param name="expectations">Additional expectations for the constructor.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations HasConstructor(Action<IConstructorExpectations>? expectations = null);

    /// <summary>
    /// Expects the class to implement an interface.
    /// </summary>
    /// <param name="interfaceName">The interface name.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations ImplementsInterface(string interfaceName);

    /// <summary>
    /// Expects the class to inherit from a base class.
    /// </summary>
    /// <param name="baseClassName">The base class name.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations InheritsFrom(string baseClassName);

    /// <summary>
    /// Expects the class to be abstract.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations IsAbstract();

    /// <summary>
    /// Expects the class to be sealed.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations IsSealed();

    /// <summary>
    /// Expects the class to be static.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations IsStatic();

    /// <summary>
    /// Expects the class to be partial.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IClassExpectations IsPartial();
}
