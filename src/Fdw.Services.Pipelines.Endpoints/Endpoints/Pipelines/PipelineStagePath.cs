using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Carries the path of a stage inside a refusal's details, and reads it back for the problem the caller is shown (FDW-1120).
/// </summary>
/// <remarks>
/// A stage inside a Branch is addressed by the names from the pipeline's own stage down: the Branch stage, the path, the stage.
/// The details hold those names joined by a separator that cannot occur in a name, so the machine path survives a name that
/// contains any visible character, including the one a dotted display uses.
/// </remarks>
internal static class PipelineStagePath
{
    /// <summary>The key of the path in a refusal's details.</summary>
    public const string DetailKey = "StagePath";

    private const char Separator = '\u001F';

    /// <summary>Joins the names of a stage path into one detail value.</summary>
    /// <param name="stagePath">The names from the outermost stage down to the stage.</param>
    /// <returns>The joined value.</returns>
    public static string Join(IReadOnlyList<string> stagePath) => string.Join(Separator, stagePath);

    /// <summary>Splits a detail value back into the names of a stage path.</summary>
    /// <param name="joined">The value <see cref="Join"/> produced.</param>
    /// <returns>The names from the outermost stage down to the stage.</returns>
    public static List<string> Split(string joined) =>
        [.. joined.Split(Separator, StringSplitOptions.RemoveEmptyEntries)];
}
