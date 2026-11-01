using System.Linq;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// Picks the first failure among a function's evaluated arguments, in argument order.
/// </summary>
public static class FirstFailure
{
    /// <summary>Answers the first failed result as the function's result.</summary>
    /// <param name="results">The arguments' results, in order; at least one has failed.</param>
    /// <returns>The first failure.</returns>
    public static IGenericResult<object?> Of(params IGenericResult[] results)
        => results.First(r => r.IsFailure).ToNewResult<object?>();
}
