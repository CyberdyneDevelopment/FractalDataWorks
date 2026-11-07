namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// The name of the value a run-based threshold reports to say which connection the run's failure came from (FDW-4192).
/// </summary>
/// <remarks>
/// The DisableConnection consequence reads the connection to disable under this name when its firing is about a pipeline and not a
/// connection. The value is the Id of the connection as text; a run whose failure named no connection reports none, and the consequence fails loudly.
/// </remarks>
public static class FailedConnectionValue
{
    /// <summary>The name of the reported value.</summary>
    public const string Name = "lastFailureConnectionId";
}
