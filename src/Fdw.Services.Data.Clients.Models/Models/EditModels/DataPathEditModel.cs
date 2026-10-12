using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Edit model for creating or updating a DataPath.
/// </summary>
public class DataPathEditModel
{
    /// <summary>Gets or sets the client-side identity for this path.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the logical name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the physical path (e.g. database schema name).</summary>
    public string PhysicalPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the collection of containers in this path.</summary>
    public IList<ContainerEditModel> Containers { get; set; } = new List<ContainerEditModel>();

    /// <summary>Creates a deep copy of this model.</summary>
    public DataPathEditModel Clone()
    {
        var clone = new DataPathEditModel
        {
            Id = Id,
            Name = Name,
            PhysicalPath = PhysicalPath,
            Description = Description,
        };
        foreach (var container in Containers)
            clone.Containers.Add(container.Clone());
        return clone;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not DataPathEditModel other)
            return false;

        return Id == other.Id
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(PhysicalPath, other.PhysicalPath, StringComparison.Ordinal)
            && string.Equals(Description, other.Description, StringComparison.Ordinal)
            && ContainersEqual(other.Containers);
    }

    private bool ContainersEqual(IList<ContainerEditModel> other)
    {
        if (Containers.Count != other.Count)
            return false;
        for (var i = 0; i < Containers.Count; i++)
            if (!Containers[i].Equals(other[i]))
                return false;
        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Name);
        hash.Add(PhysicalPath);
        hash.Add(Description);
        foreach (var container in Containers)
            hash.Add(container);
        return hash.ToHashCode();
    }
}
