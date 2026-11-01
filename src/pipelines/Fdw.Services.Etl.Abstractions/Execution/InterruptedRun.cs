namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>A run that was running when the ETL host stopped, found again when the host starts (FDW-3236).</summary>
/// <param name="Request">The request the run was accepted with.</param>
/// <param name="Restarts">How many times the host has already started this run again after a stop.</param>
public sealed record InterruptedRun(PipelineExecutionRequest Request, int Restarts);
