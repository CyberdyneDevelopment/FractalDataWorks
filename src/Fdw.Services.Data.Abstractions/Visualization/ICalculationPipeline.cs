using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Fluent builder for computed columns that executes a chain of calculations against tabular data.
/// </summary>
public interface ICalculationPipeline
{
    /// <summary>
    /// Adds a calculation step to the pipeline.
    /// </summary>
    /// <param name="calculation">The column calculation to add.</param>
    /// <returns>The pipeline instance for fluent chaining.</returns>
    ICalculationPipeline AddCalculation(ColumnCalculation calculation);

    /// <summary>
    /// Executes all registered calculations against the provided rows.
    /// Returns new rows with computed columns appended.
    /// </summary>
    /// <param name="rows">The source data rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New rows with computed columns appended.</returns>
    Task<IGenericResult<IReadOnlyList<IDataRow>>> Execute(
        IReadOnlyList<IDataRow> rows,
        CancellationToken cancellationToken = default);
}
