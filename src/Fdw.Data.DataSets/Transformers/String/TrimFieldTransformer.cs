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
/// Trims whitespace or specified characters from a string field value.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "Trim")]
public sealed class TrimFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrimFieldTransformer"/> class.
    /// </summary>
    public TrimFieldTransformer()
        : base(
            id: 300,
            name: "Trim",
            displayName: "Trim",
            description: "Trims whitespace or specified characters from both ends of a string value.",
            category: "String",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "chars",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Characters",
                HelpText = "Specific characters to trim. Defaults to whitespace if not specified."
            })
    {
    }

    /// <inheritdoc/>
    // Why: string trimming is a pure CPU operation (no I/O); Task.FromResult is honest
    // sync-returning-Task — the contract is async so future I/O-backed transformers are first-class.
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var value = input.ToString() ?? string.Empty;

        if (context.Parameters.TryGetValue("chars", out var chars) && !string.IsNullOrEmpty(chars))
        {
            return Task.FromResult(GenericResult<object?>.Success((object?)value.Trim(chars.ToCharArray())));
        }

        return Task.FromResult(GenericResult<object?>.Success((object?)value.Trim()));
    }
}
