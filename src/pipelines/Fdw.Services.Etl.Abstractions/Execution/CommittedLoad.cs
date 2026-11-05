namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// What one load destination of a cut-off run had committed to its target (FDW-3840), read from the run's records before the host decides whether to start the run again.
/// </summary>
/// <param name="Destination">The destination component's name.</param>
/// <param name="RowsCommitted">The rows it had committed; more than zero.</param>
/// <param name="RepeatsToSameResult">Whether running its load again over the same rows leaves the target as it is: true for a load that empties its target first and for a kind that says it does. False also when the records do not state it.</param>
public sealed record CommittedLoad(string Destination, long RowsCommitted, bool RepeatsToSameResult);
