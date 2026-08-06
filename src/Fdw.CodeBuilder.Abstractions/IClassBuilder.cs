using System;
using System.Collections.Generic;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Builder interface for generating class definitions.
/// </summary>
public interface IClassBuilder : ICodeBuilder
{
    /// <summary>
    /// Sets the namespace for the class.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithNamespace(string namespaceName);

    /// <summary>
    /// Adds using directives.
    /// </summary>
    /// <param name="usings">The using directives to add.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithUsings(params string[] usings);

    /// <summary>
    /// Sets the class name.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithName(string className);

    /// <summary>
    /// Sets the access modifier for the class.
    /// </summary>
    /// <param name="accessModifier">The access modifier (public, internal, etc.).</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithAccessModifier(string accessModifier);

    /// <summary>
    /// Makes the class static.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder AsStatic();

    /// <summary>
    /// Makes the class abstract.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder AsAbstract();

    /// <summary>
    /// Makes the class sealed.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder AsSealed();

    /// <summary>
    /// Makes the class partial.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder AsPartial();

    /// <summary>
    /// Sets the base class.
    /// </summary>
    /// <param name="baseClass">The base class name.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithBaseClass(string baseClass);

    /// <summary>
    /// Adds interfaces that the class implements.
    /// </summary>
    /// <param name="interfaces">The interface names.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithInterfaces(params string[] interfaces);

    /// <summary>
    /// Adds generic type parameters.
    /// </summary>
    /// <param name="typeParameters">The type parameter names.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithGenericParameters(params string[] typeParameters);

    /// <summary>
    /// Adds generic type constraints.
    /// </summary>
    /// <param name="typeParameter">The type parameter to constrain.</param>
    /// <param name="constraints">The constraints.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithGenericConstraint(string typeParameter, params string[] constraints);

    /// <summary>
    /// Adds an attribute to the class.
    /// </summary>
    /// <param name="attribute">The attribute code.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithAttribute(string attribute);

    /// <summary>
    /// Adds a field to the class.
    /// </summary>
    /// <param name="fieldBuilder">The field builder.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithField(IFieldBuilder fieldBuilder);

    /// <summary>
    /// Adds a property to the class.
    /// </summary>
    /// <param name="propertyBuilder">The property builder.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithProperty(IPropertyBuilder propertyBuilder);

    /// <summary>
    /// Adds a method to the class.
    /// </summary>
    /// <param name="methodBuilder">The method builder.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithMethod(IMethodBuilder methodBuilder);

    /// <summary>
    /// Adds a constructor to the class.
    /// </summary>
    /// <param name="constructorBuilder">The constructor builder.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithConstructor(IConstructorBuilder constructorBuilder);

    /// <summary>
    /// Adds a nested class.
    /// </summary>
    /// <param name="nestedClassBuilder">The nested class builder.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithNestedClass(IClassBuilder nestedClassBuilder);

    /// <summary>
    /// Adds XML documentation comments.
    /// </summary>
    /// <param name="summary">The summary text.</param>
    /// <returns>The builder for method chaining.</returns>
    IClassBuilder WithXmlDoc(string summary);
}
