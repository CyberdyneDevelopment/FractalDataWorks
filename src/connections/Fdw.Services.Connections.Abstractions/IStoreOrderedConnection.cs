namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Optional connection capability: the store behind the connection keeps its own order of rows from one read to the next and pages them
/// in that order itself (FDW-4911). A remote API that pages by its protocol (offset and limit, page numbers) and a file read in its row
/// order are such stores.
/// </summary>
/// <remarks>
/// <para>
/// A gateway that reads such a store as a stream pages it with Skip/Take in the store's own order: the ordering guarantee is the store's,
/// stated by the connection that knows it, not one the gateway assumes. A connection that does not declare this, and cannot open a cursor
/// (<see cref="IRecordSourceConnection"/>), is read in keyset pages by the container's declared key or refused.
/// </para>
/// <para>
/// Why a separate capability interface: the abstraction targets <c>netstandard2.0</c>, which has no default interface methods, so the
/// gateway feature-detects it as it does <see cref="IRecordSourceConnection"/>.
/// </para>
/// </remarks>
public interface IStoreOrderedConnection
{
    /// <summary>
    /// Gets the order the store keeps, in words, for the log of a read that relies on it: "the order the remote API returns", "the order
    /// of the rows in the file".
    /// </summary>
    string StoreOrder { get; }
}
