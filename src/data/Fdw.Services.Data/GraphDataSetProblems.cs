using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data;

/// <summary>Makes the problems of the check before save (FDW-5290) from a result code, or from a failure another step already answered.</summary>
public static class GraphDataSetProblems
{
    /// <summary>Makes a problem from one of the data service's result codes.</summary>
    /// <param name="codeName">The name of the result code.</param>
    /// <param name="logger">The logger the problem is written to, as the code states.</param>
    /// <param name="details">The facts the code's message names.</param>
    /// <param name="path">Where the problem is, as separate segments.</param>
    /// <returns>The problem: the code, the code's message with the facts, and the path.</returns>
    public static GraphDataSetProblem Of(string codeName, ILogger logger, IResultDetails details, params string[] path)
    {
        var code = DataServiceResultCodes.ByName(codeName);
        code.Log(logger, details);
        return new GraphDataSetProblem(code.Code, code.FormatMessage(details), path);
    }

    /// <summary>Makes a problem from a failure a step the save also runs has answered, keeping that failure's own code and message.</summary>
    /// <param name="failure">The failure.</param>
    /// <param name="path">Where the problem is, as separate segments.</param>
    /// <returns>The problem.</returns>
    public static GraphDataSetProblem Of(IGenericResult failure, params string[] path) =>
        new(failure.Code is { } code ? code.Code : string.Empty, failure.CurrentMessage ?? string.Empty, path);
}
