using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Checks a script draft the way saving it does (FDW-1442): a name, a data store, a kind that exists, the text, and parameters that
/// are named once, with a type that exists. It reads the text for nothing: whether the text is sound SQL is the database's to say.
/// </summary>
public static class ScriptChecker
{
    /// <summary>Lists every problem saving the draft would be refused for.</summary>
    /// <param name="script">The draft.</param>
    /// <returns>The problems, each with the part of the draft it concerns; empty when the draft is sound.</returns>
    public static IReadOnlyList<ScriptProblemPayload> Problems(ScriptPayload script)
    {
        ArgumentNullException.ThrowIfNull(script);
        List<ScriptProblemPayload> problems = [];
        Require(problems, script.Name, "name", "the script has no name");
        Require(problems, script.Connection, "connection", "the script does not say which data store it runs on");
        Require(problems, script.Text, "text", "the script has no text");
        if (ReferenceEquals(ScriptKinds.ByName(script.Kind), ScriptKinds.NotFound))
        {
            problems.Add(Problem("kind", $"the kind '{script.Kind}' is not a script kind; it is Query or NonQuery"));
        }

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in script.Parameters)
        {
            ParameterProblems(problems, parameter, seen);
        }

        return problems;
    }

    private static void ParameterProblems(List<ScriptProblemPayload> problems, ScriptParameterPayload parameter, HashSet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(parameter.Name))
        {
            problems.Add(Problem("parameters", "a parameter has no name"));
            return;
        }

        if (!seen.Add(parameter.Name))
        {
            problems.Add(Problem("parameters", $"the parameter '{parameter.Name}' is declared twice", parameter.Name));
        }

        if (ReferenceEquals(DataTypes.ByName(parameter.Type), DataTypes.NotFound))
        {
            problems.Add(Problem("parameters", $"the parameter '{parameter.Name}' is of type '{parameter.Type}', which is not a data type", parameter.Name, "type"));
        }
    }

    private static void Require(List<ScriptProblemPayload> problems, string value, string part, string reason)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add(Problem(part, reason));
        }
    }

    private static ScriptProblemPayload Problem(string part, string reason, params string[] below) =>
        new() { Path = [part, .. below], Reason = reason };
}
