using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Edit model for creating or updating a DataStore.
/// </summary>
public class DataStoreEditModel
{
    /// <summary>Gets or sets the unique logical name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the name of the associated Connection.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the store type (e.g. SqlServer).</summary>
    public string StoreType { get; set; } = "SqlServer";

    /// <summary>Gets or sets whether the DataStore is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the write mode (Append, Upsert, Replace, Merge).</summary>
    public string? WriteMode { get; set; }

    /// <summary>Gets or sets the collection of data paths.</summary>
    public IList<DataPathEditModel> Paths { get; set; } = new List<DataPathEditModel>();

    /// <summary>Creates a deep copy of this model.</summary>
    public DataStoreEditModel Clone()
    {
        var clone = new DataStoreEditModel
        {
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            ConnectionName = ConnectionName,
            StoreType = StoreType,
            IsActive = IsActive,
            WriteMode = WriteMode,
        };
        foreach (var path in Paths)
            clone.Paths.Add(path.Clone());
        return clone;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not DataStoreEditModel other)
            return false;

        return string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
            && string.Equals(Description, other.Description, StringComparison.Ordinal)
            && string.Equals(ConnectionName, other.ConnectionName, StringComparison.Ordinal)
            && string.Equals(StoreType, other.StoreType, StringComparison.Ordinal)
            && IsActive == other.IsActive
            && string.Equals(WriteMode, other.WriteMode, StringComparison.Ordinal)
            && PathsEqual(other.Paths);
    }

    private bool PathsEqual(IList<DataPathEditModel> other)
    {
        if (Paths.Count != other.Count)
            return false;
        for (var i = 0; i < Paths.Count; i++)
            if (!Paths[i].Equals(other[i]))
                return false;
        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        hash.Add(DisplayName);
        hash.Add(Description);
        hash.Add(ConnectionName);
        hash.Add(StoreType);
        hash.Add(IsActive);
        hash.Add(WriteMode);
        foreach (var path in Paths)
            hash.Add(path);
        return hash.ToHashCode();
    }
}
