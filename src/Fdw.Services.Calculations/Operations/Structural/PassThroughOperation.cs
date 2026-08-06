using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations.Operations.Structural;

/// <summary>
/// Returns its input unchanged, publishing it under the step's output alias.
/// </summary>
/// <remarks>
/// <para>
/// Why an identity operation earns its place: a step is the only thing that owns an output alias, so
/// pass-through is how a calculation names an input — binding a raw determinant to a stable alias
/// that later steps and the per-step trace both reference. It also gives a multi-branch calculation
/// somewhere to attach a documented no-op branch instead of a contrived arithmetic identity.
/// </para>
/// <para>
/// The value is returned exactly as supplied, with no conversion: this operation asserts nothing
/// about type, so coercing here would corrupt a value the configuration only meant to relabel.
/// A null input is a failure, not a pass-through of null — an alias bound to nothing is a hole in
/// the calculation, and every downstream reader would receive a value that is indistinguishable
/// from "deliberately empty".
/// </para>
/// </remarks>
[TypeOption(typeof(CalculationOperationTypes), "PassThrough")]
[ExcludeFromCodeCoverage]
public sealed class PassThroughOperation : CalculationOperationBase
{
    private readonly ILogger<PassThroughOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PassThroughOperation"/> class.
    /// </summary>
    public PassThroughOperation()
        : base(id: 60, name: "PassThrough", category: "Structural", description: "Returns the supplied value unchanged under this step's output alias")
    {
        _logger = NullLogger<PassThroughOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Value", Kind = "Field", IsRequired = true, DisplayName = "Value", HelpText = "The value to publish unchanged" }
        ];
    }

    /// <inheritdoc />
    public override Task<IGenericResult<object>> Calculate(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        CalculationOperationLog.OperationExecutionStarted(_logger, Name, Category);

        var value = parameters["Value"];
        if (value is null)
        {
            return Task.FromResult(GenericResult<object>.Failure(
                CalculationOperationLog.RequiredParameterMissing(_logger, "Value", Name)));
        }

        CalculationOperationLog.OperationExecutionSucceeded(_logger, Name);
        return Task.FromResult(GenericResult<object>.Success(value));
    }
}
