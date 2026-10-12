using System.Collections.Generic;
using System.Threading;
using Fdw.Calculations;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Resolves calculation inputs by fetching data from their declared sources.
/// </summary>
public interface ICalculationInputResolver
{
    /// <summary>
    /// Resolves a list of calculation inputs into their evaluated values.
    /// </summary>
    /// <param name="inputs">The declared inputs to resolve.</param>
    /// <param name="context">The calculation execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the resolved inputs or a failure message.</returns>
    Task<IGenericResult<IReadOnlyList<ResolvedCalculationInput>>> Resolve(
        IReadOnlyList<CalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken = default);
}
