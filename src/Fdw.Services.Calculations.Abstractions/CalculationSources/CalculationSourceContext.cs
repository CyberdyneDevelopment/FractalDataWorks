using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Abstractions.CalculationSources;

/// <summary>
/// Execution context handed to each <see cref="CalculationSourceTypeBase"/> option so it can resolve
/// its own catalog entries.
/// </summary>
/// <remarks>
/// Why: ships minimal on purpose — the built-in sources only need <see cref="ICalculationEntityService"/>
/// (for the Configuration source's calc.CalculationEntity reads) and a logger factory. A vendor source
/// backed by a non-calc.CalculationEntity store is a documented future extension, not built here.
/// </remarks>
/// <param name="EntityService">The live calculation entity service backing the Configuration source.</param>
/// <param name="LoggerFactory">Factory for per-source loggers.</param>
public sealed record CalculationSourceContext(
    ICalculationEntityService EntityService,
    ILoggerFactory LoggerFactory);
