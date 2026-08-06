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
/// Allocates a total in proportion to a part of a whole: (Part / Whole) * Total.
/// Returns a failure result when the whole is zero.
/// </summary>
/// <remarks>
/// Why a single operation rather than chaining Divide then Multiply: the two-step form stores the
/// intermediate ratio under its own step alias, which makes the configuration read as arithmetic
/// rather than as an allocation, and puts a rounding boundary between the divide and the multiply.
/// Evaluating it in one step keeps full decimal precision through the division and names the intent.
/// </remarks>
[TypeOption(typeof(CalculationOperationTypes), "ProportionalAllocation")]
[ExcludeFromCodeCoverage]
public sealed class ProportionalAllocationOperation : CalculationOperationBase
{
    private readonly ILogger<ProportionalAllocationOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProportionalAllocationOperation"/> class.
    /// </summary>
    public ProportionalAllocationOperation()
        : base(id: 7, name: "ProportionalAllocation", category: "Arithmetic", description: "Allocates a total in proportion to a part of a whole ((Part / Whole) * Total)")
    {
        _logger = NullLogger<ProportionalAllocationOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Part", Kind = "Field", IsRequired = true, DisplayName = "Part", HelpText = "The share being allocated for" },
            new OperationParameterDefinition { Name = "Whole", Kind = "Field", IsRequired = true, DisplayName = "Whole", HelpText = "The total the part is a share of" },
            new OperationParameterDefinition { Name = "Total", Kind = "Field", IsRequired = true, DisplayName = "Total to allocate", HelpText = "The amount being distributed" }
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
            var whole = Convert.ToDecimal(parameters["Whole"], CultureInfo.InvariantCulture);

            // Why fail rather than allocate zero: a zero whole means the allocation basis is absent,
            // and "nobody gets anything" is a different — and silently wrong — statement from "this
            // allocation cannot be computed". Matches DivideOperation's explicit guard.
            if (whole == 0m)
            {
                return Task.FromResult(GenericResult<object>.Failure(
                    CalculationOperationLog.DivisionByZero(_logger, Name)));
            }

            var result = Convert.ToDecimal(parameters["Part"], CultureInfo.InvariantCulture)
                / whole
                * Convert.ToDecimal(parameters["Total"], CultureInfo.InvariantCulture);

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
