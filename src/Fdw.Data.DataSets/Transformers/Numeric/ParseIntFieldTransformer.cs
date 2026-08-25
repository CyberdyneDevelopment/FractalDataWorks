using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// Parses a string input to int. Optionally trims specified characters before parsing.
/// Returns null when the input cannot be parsed.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "ParseInt")]
public sealed class ParseIntFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseIntFieldTransformer"/> class.
    /// </summary>
    public ParseIntFieldTransformer()
        : base(
            id: 201,
            name: "ParseInt",
            displayName: "Parse Int",
            description: "Parses a string value to int. Optional characters are trimmed before parsing. Returns null on parse failure.",
            category: "Numeric",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "trimChars",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Trim Characters",
                HelpText = "Characters to trim from the input before parsing (e.g., '$,%')."
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var text = input.ToString();
        if (text is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        if (context.Parameters.TryGetValue("trimChars", out var trimChars)
            && !string.IsNullOrEmpty(trimChars))
        {
            text = text.Trim(trimChars.ToCharArray());
        }

        if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return Task.FromResult(GenericResult<object?>.Success(result));
        }

        return Task.FromResult(GenericResult<object?>.Success(null));
    }


}
