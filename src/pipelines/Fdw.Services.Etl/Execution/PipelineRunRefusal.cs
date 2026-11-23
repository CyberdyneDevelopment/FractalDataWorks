namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Why a run is refused before it runs (FDW-6631): the code the record is closed with and the message that says why.
/// </summary>
/// <param name="Reason">The code the run is closed with.</param>
/// <param name="Message">The message that says why it was refused.</param>
internal sealed record PipelineRunRefusal(string Reason, string Message);
