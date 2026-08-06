namespace Fdw.Data.Abstractions;

/// <summary>
/// Pairs an <see cref="IContainerKey"/> that references a container with the
/// <see cref="IDataContainer"/> that owns (declares) the key.
/// </summary>
/// <remarks>
/// Stored in <see cref="IDataContainer.ReferencingKeys"/> on the REFERENCED (parent) container so
/// the cascade code can iterate inbound FK references without scanning the entire store.
/// <para>
/// Why: <c>IContainerKey</c> already carries the referenced container on <c>ReferencedContainer</c>,
/// but the key itself is owned by the child container. When iterating from the parent outward,
/// the child's identity is not available on the key alone — hence this wrapper record.
/// </para>
/// </remarks>
/// <param name="Key">The key that points at the parent container.</param>
/// <param name="Owner">The child container that declares this key.</param>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record ReferencingKeyBinding(IContainerKey Key, IDataContainer Owner);
