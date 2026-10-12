using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Messages.Attributes;

/// <summary>
/// Marks a class as a message collection base type that should have a collection generated.
/// The source generator will create a static collection class for all message types that inherit from this base.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class MessageCollectionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageCollectionAttribute"/> class.
    /// </summary>
    /// <param name="name">The name for the generated collection class.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public MessageCollectionAttribute(string name)
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