namespace Fdw.Services.Messaging;

/// <summary>
/// What a failed scheduled run is as an alert (FDW-2431): the name of its kind. The alert's identifier is the run's own.
/// </summary>
public static class ScheduleFailureAlert
{
    /// <summary>The name of the kind of alert a failed run is, which every action on its alert is recorded under.</summary>
    public const string Kind = "ScheduleFailure";
}
