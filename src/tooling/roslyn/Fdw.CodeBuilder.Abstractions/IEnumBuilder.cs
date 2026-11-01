using System;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Builder interface for generating enum definitions.
/// </summary>
public interface IEnumBuilder : ICodeBuilder
{
    /// <summary>
    /// Sets the namespace for the enum.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithNamespace(string namespaceName);

    /// <summary>
    /// Adds using directives.
    /// </summary>
    /// <param name="usings">The using directives to add.</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithUsings(params string[] usings);

    /// <summary>
    /// Sets the enum name.
    /// </summary>
    /// <param name="enumName">The enum name.</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithName(string enumName);

    /// <summary>
    /// Sets the access modifier for the enum.
    /// </summary>
    /// <param name="accessModifier">The access modifier (public, internal, etc.).</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithAccessModifier(string accessModifier);

    /// <summary>
    /// Sets the underlying type for the enum.
    /// </summary>
    /// <param name="underlyingType">The underlying type (byte, int, long, etc.).</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithUnderlyingType(string underlyingType);

    /// <summary>
    /// Adds an enum member.
    /// </summary>
    /// <param name="name">The member name.</param>
    /// <param name="value">Optional explicit value.</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithMember(string name, int? value = null);

    /// <summary>
    /// Adds an enum member with a string value representation.
    /// </summary>
    /// <param name="name">The member name.</param>
    /// <param name="value">The explicit value as a string.</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithMember(string name, string value);

    /// <summary>
    /// Adds an attribute to the enum.
    /// </summary>
    /// <param name="attribute">The attribute code.</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithAttribute(string attribute);

    /// <summary>
    /// Adds an attribute to a specific enum member.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <param name="attribute">The attribute code.</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithMemberAttribute(string memberName, string attribute);

    /// <summary>
    /// Adds XML documentation comments.
    /// </summary>
    /// <param name="summary">The summary text.</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithXmlDoc(string summary);

    /// <summary>
    /// Adds XML documentation for a specific member.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <param name="summary">The summary text.</param>
    /// <returns>The builder for method chaining.</returns>
    IEnumBuilder WithMemberXmlDoc(string memberName, string summary);
}
