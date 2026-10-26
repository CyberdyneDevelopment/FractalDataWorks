using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Evaluates a stored calculation for a query filter: the value a filter condition takes from a calculation instead of from a
/// literal.
/// </summary>
/// <remarks>
/// Why a service and not the execute route of a calculation: the route takes only the calculation's id, and a filter has to
/// supply values (a number of days, a number of seconds) that the calculation reads as its inputs. A failure names the
/// calculation and the reason; nothing is defaulted, so a query never runs with a value it did not get.
/// </remarks>
public interface IFilterCalculations
{
    /// <summary>Evaluates a calculation with the values a filter supplies for its inputs.</summary>
    /// <param name="calculation">The calculation's name.</param>
    /// <param name="inputs">The value of each input the calculation declares: a decimal for a number, a bool for a flag.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The calculation's value and its type, or the failure that names the calculation and why it could not be evaluated.</returns>
    Task<IGenericResult<FilterCalculationValue>> Evaluate(
        string calculation, IReadOnlyDictionary<string, object?> inputs, CancellationToken cancellationToken = default);
}
