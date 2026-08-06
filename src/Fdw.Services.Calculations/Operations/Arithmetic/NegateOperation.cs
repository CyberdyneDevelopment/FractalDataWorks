using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations.Operations.Arithmetic;

/// <summary>
/// Reverses the sign of a single value (-value).
/// </summary>
/// <remarks>
/// Why a first-class operation rather than Multiply-by-minus-one: a negation expressed as a
/// multiply requires a literal operand carrying "-1", which reads as a magic number in the stored
/// configuration and gives a reviewer nothing to recognise. A named unary operation states the
/// intent in the configuration itself.
/// </remarks>
[TypeOption(typeof(CalculationOperationTypes), "Negate")]
[ExcludeFromCodeCoverage]
public sealed class NegateOperation : CalculationOperationBase
{
    private readonly ILogger<NegateOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NegateOperation"/> class.
    /// </summary>
    public NegateOperation()
        : base(id: 6, name: "Negate", category: "Arithmetic", description: "Reverses the sign of a value (-value)")
    {
        _logger = NullLogger<NegateOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Value", Kind = "Field", IsRequired = true, DisplayName = "Value", HelpText = "The value whose sign is reversed" }
        ];
    }

    /// <inheritdoc />
    public override Task<IGenericResult<object>> Calculate(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        CalculationOperationLog.OperationExecutionStarted(_logger, Name, Category);

        try
        {
            var result = -Convert.ToDecimal(parameters["Value"], CultureInfo.InvariantCulture);

            CalculationOperationLog.OperationExecutionSucceeded(_logger, Name);
            return Task.FromResult(GenericResult<object>.Success(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<object>.Failure(
                CalculationOperationLog.OperationExecutionFailed(_logger, ex, Name)));
        }
    }
}
