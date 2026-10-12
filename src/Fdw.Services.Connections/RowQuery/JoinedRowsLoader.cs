using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// Loads all decoded rows for a named container in the same path/schema as the primary query, together
/// with the container itself — the callback a record-connector-based connection (FileSystem, and later
/// Http) supplies to <see cref="RecordQueryEvaluator"/> so it can resolve a <c>QueryCommand</c>'s single
/// supported JOIN target without the evaluator itself knowing how to perform transport I/O.
/// </summary>
/// <param name="containerName">The joined container's name (the JOIN target).</param>
/// <param name="cancellationToken">Cancellation token for the load.</param>
/// <returns>
/// A success result carrying the joined container and its decoded rows (<see cref="JoinedRowsResult"/>),
/// or a failure result describing why the container could not be resolved/read.
/// </returns>
/// <remarks>
/// Why the container travels with the rows (not rows alone): the evaluator validates the join's
/// parent-side column and any parent-qualified filter columns against the joined container's DECLARED
/// field schema (<see cref="RecordColumnValidator"/>), and validates the loaded rows themselves against
/// that same schema (<see cref="RecordRowValidator"/>) — the container is the one artifact both checks
/// need, and every loader already resolves it while loading the rows.
/// </remarks>
public delegate Task<IGenericResult<JoinedRowsResult>> JoinedRowsLoader(
    string containerName, CancellationToken cancellationToken);
