using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.Transformations;

/// <summary>
/// Replaces every occurrence of a text in a value with another text (FDW-1957), comparing exactly (ordinal, case sensitive).
/// </summary>
[TypeOption(typeof(TransformationTypes), "Replace")]
public sealed class ReplaceFieldTransformer : FieldTransformationBase
{
    /// <summary>Initializes a new instance of the <see cref="ReplaceFieldTransformer"/> class.</summary>
    public ReplaceFieldTransformer()
        : base(
            name: "Replace",
            displayName: "Replace",
            description: "Replaces every occurrence of a text in a value with another text.",
            category: "String",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "oldValue",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Old value",
                HelpText = "The text to find; it must not be empty."
            },
            new OperationParameterDefinition
            {
                Name = "newValue",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "New value",
                HelpText = "The text to put in its place; it may be empty."
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        TransformationContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        if (input is not string text)
        {
            throw new InvalidOperationException($"Replace expects a string input but received '{input.GetType().Name}'.");
        }

        if (!context.Parameters.TryGetValue("oldValue", out var oldValue) || string.IsNullOrEmpty(oldValue))
        {
            throw new InvalidOperationException("Replace needs the parameter 'oldValue', and it must not be empty.");
        }

        if (!context.Parameters.TryGetValue("newValue", out var newValue))
        {
            throw new InvalidOperationException("Replace needs the parameter 'newValue'; give it as empty text to remove the old value.");
        }

        return Task.FromResult(GenericResult<object?>.Success(text.Replace(oldValue, newValue, StringComparison.Ordinal)));
    }
}
