using System;

namespace Fdw.SourceGenerators.Models;

/// <summary>
/// Generalized type alias for CollectionValueInfoModel used across source generators.
/// Renamed from EnumValueInfoModel to GenericValueInfoModel for consistency.
/// </summary>
/// <typeparam name="TId">The type used for the collection ID (int for Collections, Guid for ServiceTypes).</typeparam>
public class GenericValueInfoModel<TId> : CollectionValueInfoModel<TId>, IEquatable<GenericValueInfoModel<TId>>
    where TId : struct
{
    /// <summary>
    /// Determines whether the specified <see cref="GenericValueInfoModel{TId}"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The GenericValueInfoModel to compare with the current instance.</param>
    /// <returns>true if the specified GenericValueInfoModel is equal to the current instance; otherwise, false.</returns>
    public bool Equals(GenericValueInfoModel<TId>? other)
    {
        return base.Equals(other);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is GenericValueInfoModel<TId> other && Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

/// <summary>
/// Non-generic type alias for GenericValueInfoModel using int IDs.
/// Provides backward compatibility for Collections and Messages generators.
/// </summary>
public class GenericValueInfoModel : GenericValueInfoModel<int>
{
}