namespace Fdw.Services.Data.Execution;

/// <summary>
/// What a preview of a Graph data set draft is asked for (FDW-5454).
/// </summary>
/// <param name="MaxRowsPerSource">The most rows read from each source; none states that the configured number applies.</param>
/// <param name="Component">The component whose output the rows are read at; none reads at the draft's own stated end.</param>
/// <param name="Output">The output of that component; stated with <paramref name="Component"/> or not at all.</param>
public sealed record GraphPreviewRequest(int? MaxRowsPerSource, string? Component, string? Output);
