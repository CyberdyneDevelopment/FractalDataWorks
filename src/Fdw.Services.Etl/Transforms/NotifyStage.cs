using System.Collections.Generic;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// A Notify stage found in a stage tree, with the path that addresses it and what it states.
/// </summary>
/// <param name="StagePath">The names from the outermost stage down to this stage: Branch stage, path, stage.</param>
/// <param name="StageName">The stage's own name.</param>
/// <param name="RuleName">The notification rule the stage sends through; null when it states none.</param>
/// <param name="RunPoint">The run point the stage fires at; null when it states none.</param>
/// <param name="Message">The message template the stage sends; null when it states none.</param>
public sealed record NotifyStage(
    IReadOnlyList<string> StagePath,
    string StageName,
    string? RuleName,
    string? RunPoint,
    string? Message);
