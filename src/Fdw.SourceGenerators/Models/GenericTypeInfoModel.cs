using System;

namespace Fdw.SourceGenerators.Models;

/// <summary>
/// Generalized type alias for CollectionTypeInfoModel used across source generators.
/// Renamed from EnumTypeInfoModel to GenericTypeInfoModel for consistency.
/// </summary>
/// <typeparam name="TId">The type used for collection IDs (int for Collections, Guid for ServiceTypes).</typeparam>
public class GenericTypeInfoModel<TId> : CollectionTypeInfoModel<TId>, IEquatable<GenericTypeInfoModel<TId>>
    where TId : struct
{
    /// <summary>
    /// Determines whether the specified <see cref="GenericTypeInfoModel{TId}"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The GenericTypeInfoModel to compare with the current instance.</param>
    /// <returns>true if the specified GenericTypeInfoModel is equal to the current instance; otherwise, false.</returns>
    public bool Equals(GenericTypeInfoModel<TId>? other)
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
        return obj is GenericTypeInfoModel<TId> other && Equals(other);
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
