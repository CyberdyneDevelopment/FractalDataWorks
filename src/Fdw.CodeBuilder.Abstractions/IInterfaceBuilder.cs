using System;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Builder interface for generating interface definitions.
/// </summary>
public interface IInterfaceBuilder : ICodeBuilder
{
    /// <summary>
    /// Sets the namespace for the interface.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithNamespace(string namespaceName);

    /// <summary>
    /// Adds using directives.
    /// </summary>
    /// <param name="usings">The using directives to add.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithUsings(params string[] usings);

    /// <summary>
    /// Sets the interface name.
    /// </summary>
    /// <param name="interfaceName">The interface name.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithName(string interfaceName);

    /// <summary>
    /// Sets the access modifier for the interface.
    /// </summary>
    /// <param name="accessModifier">The access modifier (public, internal, etc.).</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithAccessModifier(string accessModifier);

    /// <summary>
    /// Makes the interface partial.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder AsPartial();

    /// <summary>
    /// Adds base interfaces.
    /// </summary>
    /// <param name="interfaces">The base interface names.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithBaseInterfaces(params string[] interfaces);

    /// <summary>
    /// Adds generic type parameters.
    /// </summary>
    /// <param name="typeParameters">The type parameter names.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithGenericParameters(params string[] typeParameters);

    /// <summary>
    /// Adds generic type constraints.
    /// </summary>
    /// <param name="typeParameter">The type parameter to constrain.</param>
    /// <param name="constraints">The constraints.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithGenericConstraint(string typeParameter, params string[] constraints);

    /// <summary>
    /// Adds an attribute to the interface.
    /// </summary>
    /// <param name="attribute">The attribute code.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithAttribute(string attribute);

    /// <summary>
    /// Adds a property to the interface.
    /// </summary>
    /// <param name="propertyBuilder">The property builder.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithProperty(IPropertyBuilder propertyBuilder);

    /// <summary>
    /// Adds a method to the interface.
    /// </summary>
    /// <param name="methodBuilder">The method builder.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithMethod(IMethodBuilder methodBuilder);

    /// <summary>
    /// Adds an event to the interface.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="eventName">The event name.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithEvent(string eventType, string eventName);

    /// <summary>
    /// Adds XML documentation comments.
    /// </summary>
    /// <param name="summary">The summary text.</param>
    /// <returns>The builder for method chaining.</returns>
    IInterfaceBuilder WithXmlDoc(string summary);
}
