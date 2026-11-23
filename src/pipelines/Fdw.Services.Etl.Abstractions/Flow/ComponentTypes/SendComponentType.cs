using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Sends a notification through a notification rule, its words made from the variables and, when it states a payload type, a typed payload (a reply, a card, a summary) made from them too (FDW-6138).
/// It needs no row counts, so a run that moves no rows can send.
/// </summary>
[TypeOption(typeof(ComponentTypes), "Send")]
public sealed class SendComponentType : ComponentTypeBase
{
    /// <summary>The name of the setting that states the subject.</summary>
    public const string SubjectSetting = "subject";

    /// <summary>The name of the setting that states the payload type.</summary>
    public const string PayloadTypeSetting = "payloadType";

    /// <summary>The name of the setting whose rows state the payload's fields.</summary>
    public const string FieldsSetting = "parameters";

    /// <summary>Initializes a new instance of the <see cref="SendComponentType"/> class.</summary>
    public SendComponentType() : base(
        "Send",
        "Send",
        "Sends a notification through a notification rule: its subject and message are made from the variables, and it can carry a typed payload made from them too.",
        "Control flow",
        false,
        [
            new StageMember("rule", "Text", true, "The notification rule to send through."),
            new StageMember(SubjectSetting, "Text", true, "The subject; it may name variables in double braces, read when the step runs.", ReadsWhenStepRuns: true),
            new StageMember("message", "Text", true, "The message; it may name variables in double braces, read when the step runs."),
            new StageMember(PayloadTypeSetting, "Text", false, "The type of the payload: Reply, Card, Summary or one a package adds. A service that knows the type receives the payload; any other is sent the words."),
            new StageMember(FieldsSetting, "Parameters", false, "The payload's fields, by name, as text; a value may name variables in double braces. Needs payloadType."),
        ])
    {
    }
}
