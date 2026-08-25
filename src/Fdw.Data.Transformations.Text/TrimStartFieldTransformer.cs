using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.Transformations;

/// <summary>
/// Trims leading whitespace or specified characters from a string field value.
/// </summary>
[TypeOption(typeof(TransformationTypes), "TrimStart")]
public sealed class TrimStartFieldTransformer : FieldTransformationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrimStartFieldTransformer"/> class.
    /// </summary>
    public TrimStartFieldTransformer()
        : base(
            id: 301,
            name: "TrimStart",
            displayName: "Trim Start",
            description: "Trims leading whitespace or specified characters from a string value.",
            category: "String",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "chars",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Characters",
                HelpText = "Specific characters to trim from the start. Defaults to whitespace if not specified."
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

        var value = input.ToString() ?? string.Empty;

        if (context.Parameters.TryGetValue("chars", out var chars) && !string.IsNullOrEmpty(chars))
        {
            return Task.FromResult(GenericResult<object?>.Success(value.TrimStart(chars.ToCharArray())));
        }

        return Task.FromResult(GenericResult<object?>.Success(value.TrimStart()));
    }
}
