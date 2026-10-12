using System;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Builder interface for generating field definitions.
/// </summary>
public interface IFieldBuilder : ICodeBuilder
{
    /// <summary>
    /// Sets the field name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder WithName(string name);

    /// <summary>
    /// Sets the field type.
    /// </summary>
    /// <param name="type">The field type.</param>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder WithType(string type);

    /// <summary>
    /// Sets the access modifier.
    /// </summary>
    /// <param name="accessModifier">The access modifier.</param>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder WithAccessModifier(string accessModifier);

    /// <summary>
    /// Makes the field static.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder AsStatic();

    /// <summary>
    /// Makes the field readonly.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder AsReadOnly();

    /// <summary>
    /// Makes the field const.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder AsConst();

    /// <summary>
    /// Makes the field volatile.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder AsVolatile();

    /// <summary>
    /// Sets an initializer value.
    /// </summary>
    /// <param name="initializer">The initializer expression.</param>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder WithInitializer(string initializer);

    /// <summary>
    /// Adds an attribute to the field.
    /// </summary>
    /// <param name="attribute">The attribute code.</param>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder WithAttribute(string attribute);

    /// <summary>
    /// Adds XML documentation comments.
    /// </summary>
    /// <param name="summary">The summary text.</param>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder WithXmlDoc(string summary);

    /// <summary>
    /// Adds a preprocessor directive (e.g., "if !NET8_0_OR_GREATER") before this field.
    /// </summary>
    /// <param name="directive">The preprocessor directive without the # symbol</param>
    /// <returns>The builder for method chaining.</returns>
    IFieldBuilder WithPreprocessorDirective(string directive);
}
