using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Calculations;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Default implementation of <see cref="ICalculationInputResolver"/>.
/// Resolves inputs by kind: DataSet, Container, or Scalar.
/// </summary>
public sealed class DefaultCalculationInputResolver : ICalculationInputResolver
{
    private readonly ILogger<DefaultCalculationInputResolver> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultCalculationInputResolver"/>.
    /// </summary>
    /// <param name="logger">The logger instance. Falls back to NullLogger if null.</param>
    public DefaultCalculationInputResolver(ILogger<DefaultCalculationInputResolver>? logger)
    {
        _logger = logger ?? NullLogger<DefaultCalculationInputResolver>.Instance;
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
                    if (string.IsNullOrWhiteSpace(input.DataSetName))
                    {
                        CalculationEntityLog.InputResolutionSkipped(_logger, input.InputAlias, "DataSet name is empty");
                        continue;
                    }

                    // DataSet resolution is deferred to the execution context.
                    // The resolved value is the DataSet name for the entity type to query.
                    resolved.Add(new ResolvedCalculationInput
                    {
                        InputAlias = input.InputAlias,
                        Kind = input.Kind,
                        ResolvedValue = input.DataSetName
                    });

                    CalculationEntityLog.InputResolutionSucceeded(_logger, input.InputAlias, 1);
                }
                else if (string.Equals(kindName, "Container", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(input.ConnectionName) || string.IsNullOrWhiteSpace(input.ContainerPath))
                    {
                        CalculationEntityLog.InputResolutionSkipped(_logger, input.InputAlias, "Connection name or container path is empty");
                        continue;
                    }

                    // Container resolution is deferred to the execution context.
                    // The resolved value carries the connection+container reference for the entity type to query.
                    resolved.Add(new ResolvedCalculationInput
                    {
                        InputAlias = input.InputAlias,
                        Kind = input.Kind,
                        ResolvedValue = new ContainerReference(input.ConnectionName, input.ContainerPath)
                    });

                    CalculationEntityLog.InputResolutionSucceeded(_logger, input.InputAlias, 1);
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
}
