using System;
using System.Collections;
using System.Collections.Generic;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>
/// Describes one child relationship of a <c>[GenerateMapper]</c> parent type so the configuration
/// cascade can load and save it with NO runtime reflection. Emitted by the PocoMapper generator with
/// compile-time-closed accessors.
/// </summary>
/// <remarks>
/// Carries only layer-safe types (no gateway/connection types) — the mapper lives in
/// <c>Data.Abstractions</c>, below the gateway. The consumer (which has the gateway) performs the
/// actual fetch/save using <see cref="ChildType"/>; this descriptor supplies the reflection-free
/// parent-side accessors the cascade needs.
/// </remarks>
public interface IChildCascadeDescriptor
{
    /// <summary>
    /// The parent property bound to this child (the collection or property-collection property name).
    /// Matches the binding key the cascade resolves against the container's keys.
    /// </summary>
    string BoundPropertyName { get; }

    /// <summary>
    /// The child element .NET type — used to pick the child's mapper and the non-generic
    /// <c>Execute(command, container, Type, …)</c> overload without a closed generic.
    /// </summary>
    Type ChildType { get; }

    /// <summary>
    /// The child element type's short name — the <c>PocoMapperCollection</c> lookup key used to
    /// recurse into the child's own cascade.
    /// </summary>
    string ChildTypeName { get; }

    /// <summary>
    /// True when this child is a property-collection (key/value) bag rather than a typed-list child.
    /// </summary>
    bool IsPropertyCollection { get; }

    /// <summary>
    /// The physical foreign-key column on the CHILD table that points at this owner's <c>RowId</c>
    /// (the <c>{Owner}RowId</c> convention — e.g. <c>DataStoreRowId</c>, <c>DataPathRowId</c>,
    /// <c>MsSqlConnectionRowId</c>). The read cascade filters child rows by
    /// <c>WHERE [ChildForeignKeyColumn] = owner.RowId</c> — version-pinned to the current owner row,
    /// with no schema/container metadata lookup.
    /// </summary>
    string ChildForeignKeyColumn { get; }

    /// <summary>
    /// The child container (table) name for a property-collection (KVP) child, declared via
    /// <c>[ConfigurationChildTable]</c> (the property→table mapping is not derivable from the
    /// property/owner type). Empty for typed-list children, whose container is resolved at runtime
    /// from the child type's <c>ConfigurationCommand</c>.
    /// </summary>
    string ChildContainerName { get; }

    /// <summary>
    /// Reads the child collection from the parent instance, or <see langword="null"/> when unset.
    /// </summary>
    /// <param name="parent">The parent POCO instance.</param>
    IEnumerable? GetCollection(object parent);

    /// <summary>
    /// Sets the child collection on the parent instance (LOAD cascade).
    /// </summary>
    /// <param name="parent">The parent POCO instance.</param>
    /// <param name="collection">The materialized child collection.</param>
    void SetCollection(object parent, object? collection);

    /// <summary>
    /// Fills the parent's property-collection (key/value) property from loaded rows (LOAD cascade).
    /// No-op for typed-list children.
    /// </summary>
    /// <param name="parent">The parent POCO instance.</param>
    /// <param name="values">The loaded key/value pairs.</param>
    void FillDictionary(object parent, IDictionary<string, string?> values);

    /// <summary>Reads the parent's property-collection (key/value) bag for the WRITE cascade, or
    /// <see langword="null"/> for a typed-list child. Mirror of <see cref="FillDictionary"/>.</summary>
    IReadOnlyDictionary<string, string?>? ReadDictionary(object parent);
}
