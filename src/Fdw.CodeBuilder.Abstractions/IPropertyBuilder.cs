using System;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Builder interface for generating property definitions.
/// </summary>
public interface IPropertyBuilder : ICodeBuilder
{
    /// <summary>
    /// Sets the property name.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithName(string name);

    /// <summary>
    /// Sets the property type.
    /// </summary>
    /// <param name="type">The property type.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithType(string type);

    /// <summary>
    /// Sets the access modifier.
    /// </summary>
    /// <param name="accessModifier">The access modifier.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithAccessModifier(string accessModifier);

    /// <summary>
    /// Makes the property static.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder AsStatic();

    /// <summary>
    /// Makes the property virtual.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder AsVirtual();

    /// <summary>
    /// Makes the property override.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder AsOverride();

    /// <summary>
    /// Makes the property abstract.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder AsAbstract();

    /// <summary>
    /// Sets the property as read-only (get only).
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder AsReadOnly();

    /// <summary>
    /// Sets the property as write-only (set only).
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder AsWriteOnly();

    /// <summary>
    /// Sets a custom getter implementation.
    /// </summary>
    /// <param name="getterBody">The getter body code.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithGetter(string getterBody);

    /// <summary>
    /// Sets a custom setter implementation.
    /// </summary>
    /// <param name="setterBody">The setter body code.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithSetter(string setterBody);

    /// <summary>
    /// Sets the getter access modifier.
    /// </summary>
    /// <param name="accessModifier">The access modifier for the getter.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithGetterAccessModifier(string accessModifier);

    /// <summary>
    /// Sets the setter access modifier.
    /// </summary>
    /// <param name="accessModifier">The access modifier for the setter.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithSetterAccessModifier(string accessModifier);

    /// <summary>
    /// Sets an initializer value.
    /// </summary>
    /// <param name="initializer">The initializer expression.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithInitializer(string initializer);

    /// <summary>
    /// Makes the property use init-only setter.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithInitSetter();

    /// <summary>
    /// Adds an attribute to the property.
    /// </summary>
    /// <param name="attribute">The attribute code.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithAttribute(string attribute);

    /// <summary>
    /// Sets the property as an expression body.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithExpressionBody(string expression);

    /// <summary>
    /// Adds XML documentation comments.
    /// </summary>
    /// <param name="summary">The summary text.</param>
    /// <returns>The builder for method chaining.</returns>
    IPropertyBuilder WithXmlDoc(string summary);
}
