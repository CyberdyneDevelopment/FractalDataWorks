using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Messages.Attributes;

/// <summary>
/// Marks a class as a message option that belongs to a specific message collection.
/// The source generator will include this type in the specified collection's generated code.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class MessageOptionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageOptionAttribute"/> class.
    /// </summary>
    /// <param name="collectionType">The collection type this message option belongs to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collectionType"/> is null.</exception>
    public MessageOptionAttribute(Type collectionType)
    {
        CollectionType = collectionType ?? throw new ArgumentNullException(nameof(collectionType));
    }

    /// <summary>
    /// Gets the collection type this message option belongs to.
    /// </summary>
    public Type CollectionType { get; }
}
