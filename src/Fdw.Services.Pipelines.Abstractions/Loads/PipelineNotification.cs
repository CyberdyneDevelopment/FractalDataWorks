namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The notification a Notify stage sends (its <c>notification</c>): which rule sends, at which point of the run, and what it says.</summary>
public sealed class PipelineNotification
{
    /// <summary>Gets or sets the name of the notification rule that sends the message; the rule chooses the channel and severity, and its own conditions still decide whether it sends.</summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>Gets or sets the run point the notification fires at: an option name of the <c>RunPoints</c> collection (AfterStage, AfterLoad or OnFailure).</summary>
    public string RunPoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message, required. It may use the reserved variables of a run in double braces: <c>{{PipelineName}}</c>,
    /// <c>{{ExecutionId}}</c>, <c>{{RowsExtracted}}</c>, <c>{{RowsRejected}}</c> and <c>{{RowsLoaded}}</c>.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
