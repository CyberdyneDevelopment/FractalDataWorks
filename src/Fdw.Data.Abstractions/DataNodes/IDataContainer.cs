using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A physical data container — a table, collection, file, or API resource — that exists at a
/// known path within a data store, and is simultaneously a uniform <see cref="IDataNode"/> whose
/// children are its <see cref="IDataField"/> fields and an <see cref="IStorageContainer"/> that the
/// connection layer reads for schema, physical location, format, and supported operations.
/// </summary>
/// <remarks>
/// A built container is immediately a valid <see cref="IStorageContainer"/> (its
/// <see cref="IStorageContainer.Schema"/> is a synchronous projection over its field children) and a
/// valid <see cref="IDataNode"/> (its child <see cref="IDataNode.Nodes"/> are its fields; its
/// <see cref="Keys"/> and <see cref="ReferencingKeys"/> are present). There is no materialization step.
/// <para>
/// The tree-navigation parent of a container is its owning <see cref="Parent"/> (an
/// <see cref="IDataNodePath"/>). The physical address is the distinct <see cref="IStorageContainer.Path"/>
/// (an <see cref="IPath"/> such as <c>DatabasePath</c>/<c>HttpPath</c>/<c>FilePath</c>) read by the
/// transport translators. These are named apart so there is no <c>new</c>-hiding.
/// </para>
/// <para>
/// Consumers that need to look up a container by name and detect when it is absent should call
/// <see cref="IDataNodePath.Container"/> and check <c>IsSuccess</c> on the returned
/// <c>IGenericResult&lt;IDataContainer&gt;</c>.
/// </para>
/// </remarks>
public interface IDataContainer : IDataNode, IStorageContainer
{
    /// <summary>
    /// Gets the unique name of this container.
    /// </summary>
    // Why: IDataNode.Name and IStorageContainer.Name both declare string Name — merging them
    // here into a single declaration eliminates CS0229 ambiguity for callers.
    new string Name { get; }

    /// <summary>
    /// Gets the path within the data store that owns this container — the container's tree-navigation parent.
    /// </summary>
    /// <remarks>
    /// Why: this is the <see cref="IDataNode"/> tree back-reference (the owning <see cref="IDataNodePath"/>),
    /// named apart from the physical-address <see cref="IStorageContainer.Path"/> so there is no
    /// <c>new</c>-hiding ambiguity. Transport translators read the physical location from
    /// <see cref="IStorageContainer.Path"/>; tree navigation reads <see cref="Parent"/>.
    /// </remarks>
    IDataNodePath Parent { get; }

    /// <summary>
    /// Gets the keys defined on this container.
    /// </summary>
    /// <remarks>
    /// Why: keys are meaningful only for a storage container, so the contract lives here rather than on
    /// <see cref="IDataNode"/>. Populated at build time. Empty list when the container has no keys.
    /// </remarks>
    IReadOnlyList<IContainerKey> Keys { get; }

    /// <summary>
    /// Gets all keys on OTHER containers that reference THIS container (inbound FK references).
    /// Each entry carries both the referencing key and the child container that owns it.
    /// </summary>
    /// <remarks>
    /// Populated by the per-transport builder in a second pass after all containers and keys are built.
    /// <c>IsSuccess=false</c> when the builder was unable to compute the referencing key set for
    /// this container.
    /// <para>
    /// Why: storing the inbound set here makes the FK cascade O(1) per parent instead of scanning every
    /// container in the store, and eliminates a brittle silent-skip when key types or comparisons did
    /// not match.
    /// </para>
    /// </remarks>
    IGenericResult<IReadOnlyList<ReferencingKeyBinding>> ReferencingKeys { get; }
}
