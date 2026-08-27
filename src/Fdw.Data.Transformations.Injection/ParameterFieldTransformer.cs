using System;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.Transformations;

/// <summary>
/// Returns a named runtime value from the transform context, ignoring the input.
/// Supports "operatingDate" and "now" parameter names.
/// </summary>
[TypeOption(typeof(TransformationTypes), "Parameter")]
public sealed class ParameterFieldTransformer : FieldTransformationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterFieldTransformer"/> class.
    /// </summary>
    public ParameterFieldTransformer()
        : base(
            id: 501,
            name: "Parameter",
            displayName: "Parameter",
            description: "Returns a named runtime value from the transform context, ignoring the input.",
            category: "Injection",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "name",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Parameter Name",
                HelpText = "The name of the runtime parameter to return (e.g., 'operatingDate', 'now')."
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        TransformationContext context,
        CancellationToken cancellationToken = default)
    {
        // Why a failure and not a null: a null here reaches the row as an empty column and nothing
        // says why. The ETL caller already reports a failed transform against the field and keeps the
        // original value, so failing loud costs a message rather than a run.
        if (!context.Parameters.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(GenericResult<object?>.Failure(
                FieldTransformerLog.ParameterNameMissing(NullLogger.Instance)));
        }

        if (string.Equals(name, "operatingDate", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(GenericResult<object?>.Success((object)context.OperatingDate));
        }

        if (string.Equals(name, "now", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(GenericResult<object?>.Success((object)context.ExecutionTimestamp));
        }

        // Why the valid names are named in the message: "operatingDate" mistyped as "operatingdate"
        // works (the comparison ignores case) but "operating_date" does not, and the difference was
        // previously invisible.
        return Task.FromResult(GenericResult<object?>.Failure(
            FieldTransformerLog.ParameterNameUnknown(NullLogger.Instance, name)));
    }
}
