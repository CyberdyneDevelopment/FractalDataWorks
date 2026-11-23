using System.Collections.Generic;

namespace Fdw.Services.Etl.Flow;

/// <summary>What an Ask component states (FDW-6142), read once when the graph is built; the texts that may use variables are made from them when the step runs.</summary>
/// <param name="PolicyName">The answer policy.</param>
/// <param name="CallTemplate">The call the policy judges.</param>
/// <param name="KindName">The kind of request, one of the answer request kinds.</param>
/// <param name="ContinuationName">The pipeline the person's answer starts.</param>
/// <param name="AnswerParameterName">The continuation's parameter the answer goes to.</param>
/// <param name="RuleName">The notification rule that tells the person.</param>
/// <param name="TitleTemplate">The title the person is shown.</param>
/// <param name="BodyTemplate">The body the person is shown.</param>
/// <param name="OptionTemplates">The options of a choice; none for a question.</param>
/// <param name="GrantVariableName">The variable that holds the stored grant's id; null when the component states none.</param>
/// <param name="Arguments">The arguments the continuation is started with besides the answer.</param>
public sealed record AskStepSettings(
    string PolicyName,
    string CallTemplate,
    string KindName,
    string ContinuationName,
    string AnswerParameterName,
    string RuleName,
    string TitleTemplate,
    string BodyTemplate,
    IReadOnlyList<string> OptionTemplates,
    string? GrantVariableName,
    IReadOnlyList<FlowStepParameter> Arguments);
