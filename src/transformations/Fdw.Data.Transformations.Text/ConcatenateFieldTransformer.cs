using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.Transformations;

/// <summary>
/// Puts text before and/or after a value (FDW-1957): how a prefix and an extension are added to a name worked out from a date.
/// </summary>
[TypeOption(typeof(TransformationTypes), "Concatenate")]
public sealed class ConcatenateFieldTransformer : FieldTransformationBase
{
    /// <summary>Initializes a new instance of the <see cref="ConcatenateFieldTransformer"/> class.</summary>
    public ConcatenateFieldTransformer()
        : base(
            name: "Concatenate",
            displayName: "Concatenate",
            description: "Puts text before and/or after a value.",
            category: "String",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "prefix",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Prefix",
                HelpText = "Text put before the value."
            },
            new OperationParameterDefinition
            {
                Name = "suffix",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Suffix",
                HelpText = "Text put after the value."
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

        context.Parameters.TryGetValue("prefix", out var prefix);
        context.Parameters.TryGetValue("suffix", out var suffix);

        return Task.FromResult(GenericResult<object?>.Success(string.Concat(prefix, input.ToString(), suffix)));
    }
}
