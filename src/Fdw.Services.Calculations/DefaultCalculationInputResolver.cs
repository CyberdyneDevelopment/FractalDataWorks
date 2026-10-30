using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Calculations;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Default implementation of <see cref="ICalculationInputResolver"/>.
/// Resolves Scalar inputs to their values and DataSet inputs to the data set's rows, read as the caller; a Container input is refused,
/// because nothing reads one yet.
/// </summary>
public sealed class DefaultCalculationInputResolver : ICalculationInputResolver
{
    private readonly ILogger<DefaultCalculationInputResolver> _logger;
    private readonly IDataSetRowReader? _rowReader;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultCalculationInputResolver"/>.
    /// </summary>
    /// <param name="logger">The logger instance. Falls back to NullLogger if null.</param>
    /// <param name="rowReader">
    /// Reads a DataSet input's rows. When none is registered a DataSet input refuses with a coded failure instead of reading nothing.
    /// </param>
    public DefaultCalculationInputResolver(ILogger<DefaultCalculationInputResolver>? logger, IDataSetRowReader? rowReader = null)
    {
        _logger = logger ?? NullLogger<DefaultCalculationInputResolver>.Instance;
        _rowReader = rowReader;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<ResolvedCalculationInput>>> Resolve(
        IReadOnlyList<CalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken = default)
    {
        var resolved = new List<ResolvedCalculationInput>(inputs.Count);

        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CalculationEntityLog.InputResolutionStarted(_logger, input.InputAlias, input.Kind.Name);

            try
            {
                var kindName = input.Kind.Name;

                if (string.Equals(kindName, "Scalar", StringComparison.OrdinalIgnoreCase))
                {
                    var scalarValue = input.ScalarValue;
                    if (scalarValue is null)
                    {
                        CalculationEntityLog.InputResolutionSkipped(_logger, input.InputAlias, "Scalar value is null");
                        continue;
                    }

                    resolved.Add(new ResolvedCalculationInput
                    {
                        InputAlias = input.InputAlias,
                        Kind = input.Kind,
                        ResolvedValue = scalarValue.SerializedValue
                    });

                    CalculationEntityLog.InputResolutionSucceeded(_logger, input.InputAlias, 1);
                }
                else if (string.Equals(kindName, "DataSet", StringComparison.OrdinalIgnoreCase))
                {
                    var read = await ReadDataSet(input, context, cancellationToken).ConfigureAwait(false);
                    if (!read.IsSuccess || read.Value is not { } rows)
                    {
                        return read.ToNewResult<IReadOnlyList<ResolvedCalculationInput>>();
                    }

                    resolved.Add(new ResolvedCalculationInput
                    {
                        InputAlias = input.InputAlias,
                        Kind = input.Kind,
                        ResolvedValue = rows
                    });

                    CalculationEntityLog.InputResolutionSucceeded(_logger, input.InputAlias, rows.Count);
                }
                else if (string.Equals(kindName, "Container", StringComparison.OrdinalIgnoreCase))
                {
                    // Why a refusal: resolving the input to its name or reference handed the entity type no rows, so the
                    // calculation ran over nothing and answered success.
                    return GenericResult<IReadOnlyList<ResolvedCalculationInput>>.Failure(
                        CalculationEntityLog.InputKindNotRead(_logger, input.InputAlias, kindName));
                }
                else
                {
                    CalculationEntityLog.InputResolutionSkipped(_logger, input.InputAlias, $"Unknown input kind '{kindName}'");
                }
            }
            catch (Exception ex)
            {
                return GenericResult<IReadOnlyList<ResolvedCalculationInput>>.Failure(
                    CalculationEntityLog.InputResolutionFailed(_logger, ex, input.InputAlias));
            }
        }

        return GenericResult<IReadOnlyList<ResolvedCalculationInput>>.Success(resolved);
    }

    // The rows of a DataSet input, read through the context's gateway so the data store applies the caller's access, up to the row
    // cap the host states. A run whose host states no cap has no caller to read as, and refuses.
    private async Task<IGenericResult<IReadOnlyList<Dictionary<string, object?>>>> ReadDataSet(
        CalculationInput input, ICalculationContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.DataSetName))
            return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                CalculationEntityLog.DataSetInputNamesNoDataSet(_logger, input.InputAlias));

        if (_rowReader is null)
            return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                CalculationEntityLog.DataSetRowReaderNotRegistered(_logger, input.InputAlias));

        if (!context.Parameters.TryGetValue(CalculationDataSetRead.RowCapParameterName, out var cap) || cap is not int rowCap || rowCap <= 0)
            return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                CalculationEntityLog.DataSetInputHasNoCaller(_logger, input.InputAlias));

        return await _rowReader.Read(context.DataGateway, input.DataSetName, rowCap, cancellationToken).ConfigureAwait(false);
    }
}
