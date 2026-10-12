using System.Linq;
using Fdw.Results;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Reads the reason out of a failed result so a wrapping message can carry it (FDW-1109).
/// </summary>
public static class LoadFailureText
{
    /// <summary>Joins the messages of a failed result.</summary>
    /// <param name="result">The failed result.</param>
    /// <returns>The messages' text, separated by semicolons.</returns>
    public static string Of(IGenericResult result) => string.Join("; ", result.Messages.Select(message => message.Message));
}
