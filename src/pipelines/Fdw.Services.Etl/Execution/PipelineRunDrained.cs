namespace Fdw.Services.Etl.Execution;

/// <summary>
/// What a stopping host did with the runs in flight (FDW-3786).
/// </summary>
/// <param name="Finished">How many runs finished inside the time they were given.</param>
/// <param name="Stopped">How many were still running when the time ended and were stopped.</param>
internal sealed record PipelineRunDrained(int Finished, int Stopped);
