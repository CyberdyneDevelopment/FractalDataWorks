namespace Fdw.Services.Etl.Execution;

/// <summary>
/// How many pipeline runs may be in flight at once, as the platform's settings say (FDW-3085).
/// </summary>
/// <param name="PerServer">The most runs in flight on the whole ETL host; at least one.</param>
/// <param name="PerTenant">The most runs in flight for one tenant; at least one.</param>
public sealed record PipelineRunLimitValues(int PerServer, int PerTenant);
