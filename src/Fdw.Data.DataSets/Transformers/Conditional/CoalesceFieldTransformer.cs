using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// When the input is null, returns a fallback field value from the current record.
/// Does not support batching because it reads from <see cref="TransformationContext.CurrentRecord"/>.
/// </summary>
[TypeOption(typeof(TransformationTypes), "Coalesce")]
public sealed class CoalesceFieldTransformer : FieldTransformationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoalesceFieldTransformer"/> class.
    /// </summary>
    public CoalesceFieldTransformer()
        : base(
            id: 602,
            name: "Coalesce",
            displayName: "Coalesce",
            description: "When the input is null, returns the value of a fallback field from the current record.",
            category: "Conditional",
            supportsBatching: false,
            new OperationParameterDefinition
            {
                Name = "fallbackField",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Fallback Field",
                HelpText = "The name of the field to read from the current record when the input is null."
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        TransformationContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is not null)
        {
            return Task.FromResult(GenericResult<object?>.Success(input));
        }

        if (!context.Parameters.TryGetValue("fallbackField", out var fallbackField))
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        context.CurrentRecord.TryGetValue(fallbackField, out var fallbackValue);

        return Task.FromResult(GenericResult<object?>.Success(fallbackValue));
    }
}
