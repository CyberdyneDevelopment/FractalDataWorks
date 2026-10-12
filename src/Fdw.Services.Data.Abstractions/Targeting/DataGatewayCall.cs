using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Bundles an <see cref="IDataCommand"/> with the <see cref="DataStoreTarget"/> that identifies
/// which DataStore/Path/Container the command operates on. This is the unit that fluent builders
/// return from their terminal methods (<c>Build()</c>, <c>Value()</c>, <c>Values()</c>).
/// </summary>
/// <remarks>
/// Addressing (DataStore, Path, Container) was moved off <see cref="IDataCommand"/> and onto
/// <see cref="DataStoreTarget"/> in the target-typed-gateway refactor. <see cref="DataGatewayCall"/>
/// is the migration vehicle that keeps them together so call sites can pass a single value to
/// <see cref="IDataGateway"/> via the extension overloads in <see cref="DataGatewayCallExtensions"/>.
/// </remarks>
/// <param name="Command">The data command (query shape, filter, ordering, paging, metadata).</param>
/// <param name="Target">The DataStore/Path/Container address for this command.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public readonly record struct DataGatewayCall(IDataCommand Command, DataStoreTarget Target);
