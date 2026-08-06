using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Messages.Attributes;

/// <summary>
/// Marks a message collection for global cross-assembly discovery.
/// When this attribute is present, the source generator will scan all referenced assemblies
/// for message options, not just the current project.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Attribute class with only constructor and properties for configuration.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class GlobalMessageCollectionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalMessageCollectionAttribute"/> class.
    /// </summary>
    /// <param name="name">The name for the generated collection class.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public GlobalMessageCollectionAttribute(string name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty or whitespace.", nameof(name));

        Name = name;
        ReturnType = typeof(IGenericMessage);
    }

    /// <summary>
    /// Gets the name of the generated collection class.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the return type for generated methods.
    /// Defaults to IGenericMessage.
    /// </summary>
    public Type ReturnType { get; set; }
}