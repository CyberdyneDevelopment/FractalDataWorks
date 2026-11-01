using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Sends a notification through a notification rule once per run, with what the run has done so far in the message.</summary>
[TypeOption(typeof(ComponentTypes), "Notify")]
public sealed class NotifyComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NotifyComponentType"/> class.</summary>
    public NotifyComponentType() : base(
        "Notify",
        "Notify",
        "Sends a notification through a notification rule when the run reaches it, or only if the run fails.",
        "Control flow",
        false,
        [
            new StageMember("rule", "Text", true, "The name of the notification rule to send through; the rule chooses the channel and its own conditions still apply."),
            new StageMember("runPoint", "RunPoints", true, "When it is sent: AfterStage and AfterLoad send when the run reaches the component; OnFailure sends only if the run fails, and only on a component that is not inside a loop."),
            new StageMember("message", "Text", true, "The message, a template that may use {{PipelineName}}, {{ExecutionId}}, {{StartedAt}}, {{EndedAt}}, {{Outcome}}, {{RowsExtracted}}, {{RowsRejected}} and {{RowsLoaded}}."),
        ])
    {
    }
}
