using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Judges a call under an answer policy and, when the policy says the call needs the person, keeps an answer request for the person the run acts as, tells them through a notification rule and fires only
/// <c>asked</c> (FDW-6142). The call goes on, with no request, when the policy lets it (<c>go</c> fires), and is refused when the policy refuses it (the step fails).
/// </summary>
/// <remarks>
/// The run ends by what the author draws: the components fed from <c>go</c> run only when the call may go on, so a run whose call needs the person ends once the components fed from <c>asked</c> are done. The person's
/// answer starts the continuation pipeline the component states, once, as that person.
/// </remarks>
[TypeOption(typeof(ComponentTypes), "Ask")]
public sealed class AskComponentType : ComponentTypeBase, IDecisionComponentType
{
    /// <summary>The output that fires when the call may go on: the policy lets it, or a stored grant covers it.</summary>
    public const string Go = "go";

    /// <summary>The output that fires when a request was kept for the person and the call waits for their answer.</summary>
    public const string Asked = "asked";

    /// <summary>The name of the setting that states the answer policy.</summary>
    public const string PolicySetting = "policy";

    /// <summary>The name of the setting that states the call the policy judges.</summary>
    public const string CallSetting = "call";

    /// <summary>The name of the setting that states the kind of request, one of the answer request kinds.</summary>
    public const string KindSetting = "kind";

    /// <summary>The name of the setting that states the pipeline the answer starts.</summary>
    public const string ContinuationSetting = "continuation";

    /// <summary>The name of the setting that states the continuation's parameter the answer goes to.</summary>
    public const string AnswerParameterSetting = "answerParameter";

    /// <summary>The name of the setting that states the title the person is shown.</summary>
    public const string TitleSetting = "title";

    /// <summary>The name of the setting that states the body the person is shown.</summary>
    public const string BodySetting = "body";

    /// <summary>The name of the setting that lists the options of a choice, one on each line.</summary>
    public const string OptionsSetting = "options";

    /// <summary>The name of the setting that holds the name of the variable that holds the id of the stored grant to judge a call against.</summary>
    public const string GrantVariableSetting = "grantVariable";

    /// <summary>The name of the setting whose rows state the arguments the continuation is started with besides the answer.</summary>
    public const string ArgumentsSetting = "parameters";

    /// <summary>The character that separates the options in the <c>options</c> setting.</summary>
    public const char OptionSeparator = '\n';

    /// <summary>Reads the options of a choice.</summary>
    /// <param name="text">The text of the <c>options</c> setting.</param>
    /// <returns>The options, trimmed, without the empty lines.</returns>
    public static IReadOnlyList<string> OptionsOf(string text)
    {
        // Why by hand: this package targets netstandard2.0, whose Split does not trim.
        List<string> options = [];
        foreach (var part in text.Split(OptionSeparator))
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                options.Add(trimmed);
            }
        }

        return options;
    }

    /// <summary>Initializes a new instance of the <see cref="AskComponentType"/> class.</summary>
    public AskComponentType() : base(
        "Ask",
        "Ask a person",
        "Judges a call under an answer policy; when the call needs the person it keeps an answer request for the person the run acts as, tells them through a notification rule and fires only 'asked', and the run goes on from there only along what the author drew. The person's answer starts the continuation pipeline once.",
        "Control flow",
        false,
        [
            new StageMember("rule", "Text", true, "The notification rule that tells the person about the request."),
            new StageMember(PolicySetting, "Text", true, "The answer policy that says which calls need the person, which a stored grant covers and how long a request lasts."),
            new StageMember(CallSetting, "Text", true, "The call the policy judges, as the policy lists it; it may use variables in double braces, read when the step runs.", ReadsWhenStepRuns: true),
            new StageMember(KindSetting, "Text", true, "The kind of request: Question (the person answers in words) or Choice (the person answers with one of the options)."),
            new StageMember(ContinuationSetting, "Text", true, "The pipeline the person's answer starts, once, as that person."),
            new StageMember(AnswerParameterSetting, "Text", true, "The continuation's parameter that receives the answer."),
            new StageMember(TitleSetting, "Text", true, "The title the person is shown; it may use variables in double braces, read when the step runs.", ReadsWhenStepRuns: true),
            new StageMember(BodySetting, "Text", true, "The body the person is shown; it may use variables in double braces, read when the step runs.", ReadsWhenStepRuns: true),
            new StageMember(OptionsSetting, "Text", false, "The options of a choice, one on each line; a line may use variables in double braces, read when the step runs. A question takes none.", ReadsWhenStepRuns: true),
            new StageMember(GrantVariableSetting, "Text", false, "The variable that holds the id of the stored grant to judge a grant-or-person call against; without it such a call is left to the person."),
            new StageMember(ArgumentsSetting, "Parameters", false, "The arguments the continuation is started with besides the answer, by name, as text; a value may use variables in double braces."),
        ])
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<string> FiredOutputs(Func<string, string?> stated) => [Go, Asked];
}
