using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Edit model for creating or updating a DataContainer (table or view).
/// </summary>
public class ContainerEditModel
{
    /// <summary>Gets or sets the client-side identity for this container.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the logical name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the physical name (e.g. table name in the database).</summary>
    public string PhysicalName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type (e.g. Table, View).</summary>
    public string ContainerType { get; set; } = "Table";

    /// <summary>Gets or sets the collection of fields in this container.</summary>
    public IList<FieldEditModel> Fields { get; set; } = new List<FieldEditModel>();

    /// <summary>Creates a deep copy of this model.</summary>
    public ContainerEditModel Clone()
    {
        var clone = new ContainerEditModel
        {
            Id = Id,
            Name = Name,
            PhysicalName = PhysicalName,
            ContainerType = ContainerType,
        };
        foreach (var field in Fields)
            clone.Fields.Add(field.Clone());
        return clone;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not ContainerEditModel other)
            return false;

        return Id == other.Id
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(PhysicalName, other.PhysicalName, StringComparison.Ordinal)
            && string.Equals(ContainerType, other.ContainerType, StringComparison.Ordinal)
            && FieldsEqual(other.Fields);
    }

    private bool FieldsEqual(IList<FieldEditModel> other)
    {
        if (Fields.Count != other.Count)
            return false;
        for (var i = 0; i < Fields.Count; i++)
            if (!Fields[i].Equals(other[i]))
                return false;
        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Name);
        hash.Add(PhysicalName);
        hash.Add(ContainerType);
        foreach (var field in Fields)
            hash.Add(field);
        return hash.ToHashCode();
    }
}
