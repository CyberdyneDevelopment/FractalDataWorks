using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Turns what a dry-run check finds into <see cref="PipelineProblem"/> entries the Pipeline Builder can place on a
/// stage and a field (FDW-1095).
/// </summary>
public static class PipelineProblems
{
    /// <summary>Turns a refused stage mapping into the problem that places it on the stage it names, nested stages included.</summary>
    /// <param name="refusal">The failure the stage mapper returned.</param>
    /// <param name="whilst">What the server was doing, for a refusal that gave no reason.</param>
    /// <returns>A problem on the stage whose path the refusal carries, or on the pipeline's stages as a whole when it carries none.</returns>
    public static PipelineProblem OfStageRefusal(IGenericResult refusal, string whilst)
    {
        var reason = ReasonOf(refusal.CurrentMessage, whilst);
        var joined = refusal.Details?.GetValue<string>(PipelineStagePath.DetailKey);
        if (string.IsNullOrEmpty(joined))
        {
            return OfPipeline("Transforms", reason);
        }

        var path = PipelineStagePath.Split(joined);
        return new PipelineProblem { Stage = string.Join('.', path), StagePath = path, Field = null, Reason = reason };
    }

    /// <summary>Builds a problem that belongs to a stage, nested stages included, and optionally to a field of it.</summary>
    /// <param name="stagePath">The names from the pipeline's own stage down to the stage: stage, path, stage and so on.</param>
    /// <param name="field">The field of the stage the problem is about, or <see langword="null"/>.</param>
    /// <param name="reason">What is wrong.</param>
    /// <returns>The problem, placed on the stage by its path and by its dotted display form.</returns>
    public static PipelineProblem OfStage(IReadOnlyList<string> stagePath, string? field, string reason) =>
        new() { Stage = string.Join('.', stagePath), StagePath = [.. stagePath], Field = field, Reason = reason };

    /// <summary>Builds a problem that belongs to the pipeline as a whole.</summary>
    /// <param name="field">The request property the problem is about, or <see langword="null"/>.</param>
    /// <param name="reason">What is wrong.</param>
    /// <returns>The problem.</returns>
    public static PipelineProblem OfPipeline(string? field, string reason) =>
        new() { Stage = null, Field = field, Reason = reason };

    /// <summary>The reason to give for a failed check: its own message, or an honest statement that the server gave none.</summary>
    /// <param name="message">The failure's message, or <see langword="null"/> when it has none.</param>
    /// <param name="whilst">What the server was doing, for the sentence used when there is no message.</param>
    /// <returns>The reason.</returns>
    /// <remarks>
    /// A failure that gives no reason is a server fault, logged where it happened; the caller is told that and nothing else
    /// is invented for it.
    /// </remarks>
    public static string ReasonOf(string? message, string whilst) =>
        message is { Length: > 0 } reason
            ? reason
            : $"The server failed while {whilst} and gave no reason; see the server log";
}
