using Fdw.Results;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>Builds the refusal a caller is answered with when its query does not resolve.</summary>
/// <remarks>
/// The translation is the same for a data set and a configuration resource, but what a refusal is
/// called, and what it says about where it came from, belongs to the route: a data set's refusal
/// names the data set, a resource's names the resource.
/// </remarks>
public interface IQueryRefusals
{
    /// <summary>Refuses a query, saying why.</summary>
    /// <typeparam name="T">The result type the refusal is returned as.</typeparam>
    /// <param name="reason">The first thing in the query that did not resolve.</param>
    /// <returns>The failure.</returns>
    IGenericResult<T> Invalid<T>(string reason);
}
