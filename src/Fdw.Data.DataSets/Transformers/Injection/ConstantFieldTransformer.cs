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
/// Returns a literal constant value, ignoring the input.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "Constant")]
public sealed class ConstantFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConstantFieldTransformer"/> class.
    /// </summary>
    public ConstantFieldTransformer()
        : base(
            id: 500,
            name: "Constant",
            displayName: "Constant",
            description: "Returns a literal constant value, ignoring the input.",
            category: "Injection",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "value",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Value",
                HelpText = "The constant value to return."
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Execute(
        object? input,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        parameters.TryGetValue("value", out var value);

        return Task.FromResult(GenericResult<object?>.Success(value));
    }
}
