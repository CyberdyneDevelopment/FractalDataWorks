using System;

namespace Fdw.Collections;

/// <summary>
/// Represents a strongly-typed lookup request with a typed optional identifier.
/// Domain request records (e.g. a ConnectionRequest vs a DataStoreRequest) implement this
/// so lookup surfaces can distinguish what kind of thing is being requested by type
/// rather than by bare string/Guid overloads.
/// </summary>
/// <typeparam name="TId">The identifier type. Must be a value type implementing IEquatable (e.g. int, Guid).</typeparam>
/// <typeparam name="TSelf">The implementing type, used for self-referencing generics pattern (CRTP).</typeparam>
/// <remarks>
/// Excluded from code coverage: Interface with no implementation code.
/// A request carrying neither Id nor Name is invalid — consumers fail loud with a structured
/// result; there is no fallback resolution.
/// </remarks>
public interface ITypeRequest<TId, TSelf> : ITypeRequest<TSelf>
    where TId : struct, IEquatable<TId>
    where TSelf : ITypeRequest<TId, TSelf>
{
    /// <summary>
    /// Gets the strongly-typed optional identifier being requested.
    /// Hides the base interface's object Id property with the strongly-typed version.
    /// </summary>
    new TId? Id { get; }
}

/// <summary>
/// Represents a self-typed lookup request without a specific identifier type.
/// </summary>
/// <typeparam name="TSelf">The implementing type, used for self-referencing generics pattern (CRTP).</typeparam>
/// <remarks>
/// Excluded from code coverage: Interface with no implementation code.
/// </remarks>
public interface ITypeRequest<TSelf> : ITypeRequest
    where TSelf : ITypeRequest<TSelf>
{
}

/// <summary>
/// Base interface for lookup requests with object-typed Id for non-generic code.
/// A request identifies the thing being looked up by Id and/or Name; both are optional,
/// but at least one must be supplied — consumers fail loud on an empty request.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Interface with no implementation code.
/// </remarks>
public interface ITypeRequest
{
    /// <summary>
    /// Gets the optional identifier being requested (boxed as object).
    /// Use ITypeRequest&lt;TId, TSelf&gt; for strongly-typed access.
    /// </summary>
    object? Id { get; }

    /// <summary>
    /// Gets the optional name being requested.
    /// </summary>
    string? Name { get; }
}
