using System;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The owner of a described thing that belongs to one: the data set of a data set field.</summary>
public sealed class DescriptionOwner
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionOwner"/> class.</summary>
    /// <param name="id">The owner's logical Id, by which the descriptions of all its children are found.</param>
    /// <param name="name">The owner's name, which a search shows beside the thing's own.</param>
    public DescriptionOwner(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>Gets the owner's logical Id.</summary>
    public Guid Id { get; }

    /// <summary>Gets the owner's name.</summary>
    public string Name { get; }
}
