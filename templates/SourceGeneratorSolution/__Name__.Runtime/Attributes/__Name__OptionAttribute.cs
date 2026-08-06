using System;

namespace __RootNamespace__.__Name__.Attributes;

/// <summary>
/// Marks a class as a __Name__ option that should be discovered by the source generator.
/// </summary>
/// <remarks>
/// This attribute is used to automatically generate collection and provider classes.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class __Name__OptionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="__Name__OptionAttribute"/> class.
    /// </summary>
    /// <param name="collectionType">The collection type this option belongs to.</param>
    /// <param name="name">The unique name for this option.</param>
    public __Name__OptionAttribute(Type collectionType, string name)
    {
        CollectionType = collectionType ?? throw new ArgumentNullException(nameof(collectionType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Gets the collection type this option belongs to.
    /// </summary>
    public Type CollectionType { get; }

    /// <summary>
    /// Gets the unique name for this option.
    /// </summary>
    public string Name { get; }
}
