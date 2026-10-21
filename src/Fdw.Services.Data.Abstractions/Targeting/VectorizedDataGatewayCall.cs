using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Bundles a vectorized command with the <see cref="DataStoreTarget"/> it runs against: what the vectorized builders
/// (<c>Vectorized.Nearest</c>) return from <c>Build()</c>, as <see cref="DataGatewayCall"/> is for data commands.
/// </summary>
/// <typeparam name="TResult">What the command answers with.</typeparam>
/// <param name="Command">The vectorized command.</param>
/// <param name="Target">The DataStore/Path/Container address for this command.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public readonly record struct VectorizedDataGatewayCall<TResult>(IVectorizedDataCommand<TResult> Command, DataStoreTarget Target);
