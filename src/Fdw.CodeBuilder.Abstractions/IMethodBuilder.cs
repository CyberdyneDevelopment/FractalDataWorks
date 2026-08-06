using System;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Builder interface for generating method definitions.
/// </summary>
public interface IMethodBuilder : ICodeBuilder
{
    /// <summary>
    /// Sets the method name.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithName(string name);

    /// <summary>
    /// Sets the return type.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithReturnType(string returnType);

    /// <summary>
    /// Sets the access modifier.
    /// </summary>
    /// <param name="accessModifier">The access modifier.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithAccessModifier(string accessModifier);

    /// <summary>
    /// Makes the method static.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder AsStatic();

    /// <summary>
    /// Makes the method virtual.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder AsVirtual();

    /// <summary>
    /// Makes the method override.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder AsOverride();

    /// <summary>
    /// Makes the method abstract.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder AsAbstract();

    /// <summary>
    /// Makes the method async.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder As();

    /// <summary>
    /// Makes the method hide a base member with the 'new' keyword.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder AsNew();

    /// <summary>
    /// Adds a parameter to the method.
    /// </summary>
    /// <param name="type">The parameter type.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="defaultValue">Optional default value.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithParameter(string type, string name, string? defaultValue = null);

    /// <summary>
    /// Adds generic type parameters.
    /// </summary>
    /// <param name="typeParameters">The type parameter names.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithGenericParameters(params string[] typeParameters);

    /// <summary>
    /// Adds generic type constraints.
    /// </summary>
    /// <param name="typeParameter">The type parameter to constrain.</param>
    /// <param name="constraints">The constraints.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithGenericConstraint(string typeParameter, params string[] constraints);

    /// <summary>
    /// Adds an attribute to the method.
    /// </summary>
    /// <param name="attribute">The attribute code.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithAttribute(string attribute);

    /// <summary>
    /// Sets the method body.
    /// </summary>
    /// <param name="body">The method body code.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithBody(string body);

    /// <summary>
    /// Adds a line to the method body.
    /// </summary>
    /// <param name="line">The line of code to add.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder AddBodyLine(string line);

    /// <summary>
    /// Sets the method as an expression body.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithExpressionBody(string expression);

    /// <summary>
    /// Adds XML documentation comments.
    /// </summary>
    /// <param name="summary">The summary text.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithXmlDoc(string summary);

    /// <summary>
    /// Adds XML documentation for a parameter.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="description">The parameter description.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithParamDoc(string parameterName, string description);

    /// <summary>
    /// Adds XML documentation for the return value.
    /// </summary>
    /// <param name="description">The return value description.</param>
    /// <returns>The builder for method chaining.</returns>
    IMethodBuilder WithReturnDoc(string description);
}
