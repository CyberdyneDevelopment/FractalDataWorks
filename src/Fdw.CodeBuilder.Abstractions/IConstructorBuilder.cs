using System;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Builder interface for generating constructor definitions.
/// </summary>
public interface IConstructorBuilder : ICodeBuilder
{
    /// <summary>
    /// Sets the access modifier.
    /// </summary>
    /// <param name="accessModifier">The access modifier.</param>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder WithAccessModifier(string accessModifier);

    /// <summary>
    /// Makes the constructor static.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder AsStatic();

    /// <summary>
    /// Adds a parameter to the constructor.
    /// </summary>
    /// <param name="type">The parameter type.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="defaultValue">Optional default value.</param>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder WithParameter(string type, string name, string? defaultValue = null);

    /// <summary>
    /// Adds a base constructor call.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the base constructor.</param>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder WithBaseCall(params string[] arguments);

    /// <summary>
    /// Adds a this constructor call.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the this constructor.</param>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder WithThisCall(params string[] arguments);

    /// <summary>
    /// Adds an attribute to the constructor.
    /// </summary>
    /// <param name="attribute">The attribute code.</param>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder WithAttribute(string attribute);

    /// <summary>
    /// Sets the constructor body.
    /// </summary>
    /// <param name="body">The constructor body code.</param>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder WithBody(string body);

    /// <summary>
    /// Adds a line to the constructor body.
    /// </summary>
    /// <param name="line">The line of code to add.</param>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder AddBodyLine(string line);

    /// <summary>
    /// Adds XML documentation comments.
    /// </summary>
    /// <param name="summary">The summary text.</param>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder WithXmlDoc(string summary);

    /// <summary>
    /// Adds XML documentation for a parameter.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="description">The parameter description.</param>
    /// <returns>The builder for method chaining.</returns>
    IConstructorBuilder WithParamDoc(string parameterName, string description);
}
